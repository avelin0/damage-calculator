using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Contém as estruturas de dados e a lógica central para a simulação de dano de artilharia,
/// incluindo o cálculo estocástico de Probabilidade de Acerto (Pk) e a aplicação de dano (Ph/k)
/// baseado na atenuação e na vulnerabilidade do alvo.
/// </summary>
public static class SimulacaoDano
{
    /// <summary>
    /// Define as características físicas e balísticas de uma munição de artilharia.
    /// </summary>
    public class Municao
    {
        /// <summary>
        /// Nome do calibre da munição (ex: "Tiro 155mm").
        /// </summary>
        public string Calibre { get; set; } = string.Empty; 
        
        /// <summary>
        /// Raio máximo de detonação para o cálculo de atenuação de dano (em metros).
        /// </summary>
        public int RaioDetonacaoM { get; set; } 
        
        /// <summary>
        /// Equivalente à massa de TNT (em Kg) para fins de cálculo de energia.
        /// </summary>
        public double TntKg { get; set; } 
        
        /// <summary>
        /// Custo unitário da munição para análises de custo-eficácia.
        /// </summary>
        public double CustoUnitario { get; set; }
        
        /// <summary>
        /// Dano bruto que esta munição causa no epicentro (Posição 0m), em Pontos de Vida (PV).
        /// </summary>
        public double DanoEpicentroPV { get; set; } 
        
        /// <summary>
        /// Raio de Arma (Weapon Radius) no alcance (Range), usado em modelos de dano analítico (ex: Carlton).
        /// </summary>
        public double WRRange { get; set; } 
        
        /// <summary>
        /// Raio de Arma (Weapon Radius) na deflexão, usado em modelos de dano analítico (ex: Carlton).
        /// </summary>
        public double WRDeflection { get; set; } 
    }

    /// <summary>
    /// Define as propriedades de resistência, estado de vida e vulnerabilidade do alvo.
    /// </summary>
    public class Alvo
    {
        /// <summary>
        /// Nome identificador do alvo (ex: "Seç AAAe").
        /// </summary>
        public string Nome { get; set; } = string.Empty; 
        
        /// <summary>
        /// Vida atual do alvo. Começa em VidaMaxima e é decrementada pelo dano.
        /// </summary>
        public double VidaAtual { get; set; }
        
        /// <summary>
        /// Vida máxima do objeto (Pontos de Vida - PDV).
        /// </summary>
        public double VidaMaxima { get; set; } 
        
        /// <summary>
        /// Valor de Blindagem/Resistência (TVA) do alvo. Usado como referência para calcular o fator de escala de dano.
        /// </summary>
        public int BlindagemTVA { get; set; } 
        
        /// <summary>
        /// Exposição do alvo no terreno (0.0 a 1.0). Afeta a probabilidade de acerto (Pk).
        /// </summary>
        public double Exposicao { get; set; }  
        
        /// <summary>
        /// Posição do alvo em relação ao ponto de impacto (em metros). Usado para atenuação.
        /// </summary>
        public double PosicaoM { get; set; } 
        
        /// <summary>
        /// Fator de calibração que garante que a dosagem de fogo da referência (TMQ) seja atingida no epicentro.
        /// </summary>
        public double DanoEscalaFator { get; set; } 
    }

    // ----------------------------------------------
    // Funções de Cálculo de Probabilidade (Pk)
    // ----------------------------------------------
    /// <summary>
    /// Calcula a Probabilidade de Acerto (Pk) com base no modelo de queda linear simplificado.
    /// </summary>
    /// <param name="d">Distância do alvo ao epicentro (m).</param>
    /// <param name="Rdeton">Raio máximo de detonação (m).</param>
    /// <param name="E">Exposição do alvo (0.0 a 1.0).</param>
    /// <returns>A probabilidade de acerto Pk (0 a 100).</returns>
    public static double CalcularPk(double d, int Rdeton, double E)
    {
        if (d >= Rdeton) return 0;
        double Pk_min_exposto = 10.0 * E; 
        double Pk = 100.0 - (100.0 - Pk_min_exposto) * (d / Rdeton);
        return Math.Clamp(Pk, 0, 100);
    }

    // ----------------------------------------------
    // Funções de Cálculo de Dano (Ph/k)
    // ----------------------------------------------
    /// <summary>
    /// Aplica o dano real (Ph/k) ao alvo com base na atenuação da explosão e no fator de escala.
    /// </summary>
    /// <param name="alvo">O alvo a ser atingido.</param>
    /// <param name="municao">A munição utilizada.</param>
    /// <returns>O valor de dano efetivo aplicado ao alvo.</returns>
    public static double AplicarDano(Alvo alvo, Municao municao)
    {
        double d = alvo.PosicaoM;
        double Rdeton = municao.RaioDetonacaoM;

        if (d >= Rdeton) return 0;

        /// <summary>1. Cálculo da Atenuação da Explosão (0.0 a 1.0)</summary>
        double fator_proximidade = 1.0 - Math.Pow((d / Rdeton), 2);
        
        /// <summary>2. Dano Bruto no Epicentro (PDV) * Atenuação</summary>
        double danoBruto = municao.DanoEpicentroPV;
        double danoAtenuado = danoBruto * fator_proximidade;
        
        /// <summary>3. Dano Final Calibrado (Ajuste pela dosagem de fogo)</summary>
        double danoFinal = danoAtenuado * alvo.DanoEscalaFator;

        double danoEfetivo = Math.Clamp(danoFinal, 0, alvo.VidaAtual);
        
        alvo.VidaAtual -= danoEfetivo;
        return danoEfetivo;
    }
    
    /// <summary>
    /// Calcula a probabilidade de dano (Pk) usando o modelo Carlton/Meyer (decaimento exponencial).
    /// Esta função é mantida para fins de referência analítica e não é utilizada na simulação de dano principal (AplicarDano).
    /// </summary>
    /// <param name="x">Distância no alcance (Range) do ponto de impacto (m).</param>
    /// <param name="y">Distância na deflexão do ponto de impacto (m).</param>
    /// <param name="WRr">Raio de Arma no alcance.</param>
    /// <param name="WRd">Raio de Arma na deflexão.</param>
    /// <returns>A probabilidade de acerto (Pk) (0 a 1).</returns>
    public static double CalcularDanoCarlton(double x, double y, double WRr, double WRd)
    {
        double termoX = Math.Pow(x / WRr, 2);
        double termoY = Math.Pow(y / WRd, 2);
        double pkCarlton = Math.Exp(-(termoX + termoY));
        return Math.Clamp(pkCarlton, 0, 1);
    }
}

/// <summary>
/// Classe principal que inicializa o modelo de simulação, define os alvos e munições,
/// e executa as tabelas de validação e análise de sensibilidade no console.
/// </summary>
public class Program
{
    // --- 1. DADOS DAS MUNIÇÕES (Definições de Dano no Epicentro) ---
    private const double DANO_155MM_EPICENTRO = 500.0;
    private const double DANO_105MM_EPICENTRO = 166.0;
    private const double DANO_81MM_EPICENTRO = 84.0;
    private const double DANO_60MM_EPICENTRO = 42.0;

    /// <summary>
    /// Dicionário contendo as especificações detalhadas de cada munição utilizada.
    /// </summary>
    private static readonly Dictionary<string, SimulacaoDano.Municao> Municoes = new Dictionary<string, SimulacaoDano.Municao>
    {
        ["155mm"] = new SimulacaoDano.Municao { Calibre = "Tiro 155mm", RaioDetonacaoM = 25, TntKg = 7.0, CustoUnitario = 100, DanoEpicentroPV = DANO_155MM_EPICENTRO, WRRange = 70.00, WRDeflection = 70.00 },
        ["105/120mm"] = new SimulacaoDano.Municao { Calibre = "Tiro 105/120m", RaioDetonacaoM = 20, TntKg = 2.0, CustoUnitario = 40, DanoEpicentroPV = DANO_105MM_EPICENTRO, WRRange = 50.00, WRDeflection = 50.00 },
        ["81mm"] = new SimulacaoDano.Municao { Calibre = "Tiro 81m", RaioDetonacaoM = 15, TntKg = 1.0, CustoUnitario = 10, DanoEpicentroPV = DANO_81MM_EPICENTRO, WRRange = 45.00, WRDeflection = 45.00 },
        ["60mm"] = new SimulacaoDano.Municao { Calibre = "Tiro 60mm", RaioDetonacaoM = 10, TntKg = 0.5, CustoUnitario = 5, DanoEpicentroPV = DANO_60MM_EPICENTRO, WRRange = 30.00, WRDeflection = 30.00 },
        ["HeavyRocket"] = new SimulacaoDano.Municao { Calibre = "Foguete Pesado", RaioDetonacaoM = 50, TntKg = 15.0, CustoUnitario = 500, DanoEpicentroPV = 800.0, WRRange = 90.00, WRDeflection = 120.00 }
    };
    
    // --- 2. DOSAGEM DE FOGO E VIDA (PDV) (Tabela de Referência TMQ) ---
    /// <summary>
    /// Tabela de referência contendo a dosagem de tiros de 81mm e a vida total (PV) de cada alvo.
    /// </summary>
    private static readonly Dictionary<string, (int Tiros81, int VidaPV)> AlvosTabela = new Dictionary<string, (int, int)>
    {
        {"Seç AAAe", (12, 1000)}, {"Bia AP", (12, 1000)}, {"Bia AR", (12, 1000)},
        {"Pos Metralhadora", (12, 1000)}, {"PO", (6, 500)}, {"Pel Inf Mec", (12, 1000)},
        {"Pel CC", (12, 1000)}, {"AT 300X100", (12, 1000)}, {"PC 200X100", (8, 667)},
        {"Pos Def – Esp Vtr", (12, 1000)}, {"Cln Vtr 250X100", (8, 667)}, {"Pos Morteiro", (12, 1000)}
    };

    // --- 3. CÁLCULO DO FATOR DE ESCALA DE DANO ---
    /// <summary>
    /// Dicionário contendo as instâncias de Alvo inicializadas e calibradas com o Fator de Escala.
    /// </summary>
    private static readonly Dictionary<string, SimulacaoDano.Alvo> AlvosBase = InicializarAlvos();

    /// <summary>
    /// Inicializa e calibra os alvos usando a dosagem de 81mm como referência.
    /// </summary>
    /// <returns>O dicionário de alvos calibrados.</returns>
    private static Dictionary<string, SimulacaoDano.Alvo> InicializarAlvos()
    {
        var alvos = new Dictionary<string, SimulacaoDano.Alvo>();
        var municaoRef = Municoes["81mm"];
        
        foreach (var par in AlvosTabela)
        {
            var nome = par.Key;
            var (tirosRef, vidaPV) = par.Value;

            double danoRequeridoPorTiro = (double)vidaPV / tirosRef; 

            double fatorEscala = danoRequeridoPorTiro / municaoRef.DanoEpicentroPV;

            int blindagemRef = (int)Math.Round(vidaPV / 10.0);

            alvos.Add(nome, new SimulacaoDano.Alvo
            {
                Nome = nome,
                VidaMaxima = vidaPV,
                VidaAtual = vidaPV,
                BlindagemTVA = blindagemRef, 
                Exposicao = 0.5, 
                DanoEscalaFator = fatorEscala
            });
        }
        return alvos;
    }

    /// <summary>
    /// Ponto de entrada principal da aplicação. Executa as tabelas de validação e sensibilidade.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("--- VALIDAÇÃO DO MODELO DE DANO (ALVOS E MUNIÇÕES) ---");
        Console.WriteLine("------------------------------------------------------\n");

        ExibirTabelaComparativaPorMunicao();
        
        ExibirTabelaValidacaoFinal();

        ExibirTabelaSensibilidade("Seç AAAe"); 
    }
    
    /// <summary>
    /// Simula o número de tiros necessários para destruir um alvo no epicentro (Posição 0m).
    /// </summary>
    /// <param name="alvo">O alvo a ser testado (será clonado internamente).</param>
    /// <param name="municao">A munição utilizada.</param>
    /// <returns>O número inteiro de tiros necessários para zerar a vida do alvo.</returns>
    private static int SimularTirosParaDestruicao(SimulacaoDano.Alvo alvo, SimulacaoDano.Municao municao)
    {
        var alvoTeste = new SimulacaoDano.Alvo 
        {
            Nome = alvo.Nome,
            BlindagemTVA = alvo.BlindagemTVA,
            Exposicao = alvo.Exposicao,
            VidaMaxima = alvo.VidaMaxima, 
            VidaAtual = alvo.VidaMaxima, // Vida máxima no início
            PosicaoM = 0.0, // Sempre no epicentro para dosagem
            DanoEscalaFator = alvo.DanoEscalaFator
        };

        int contadorTiros = 0;
        
        while (alvoTeste.VidaAtual > 0)
        {
            SimulacaoDano.AplicarDano(alvoTeste, municao);
            contadorTiros++;
            if (contadorTiros > 200) break; 
        }

        return contadorTiros;
    }
    
    /// <summary>
    /// Exibe a tabela de comparação da dosagem de fogo no epicentro entre diferentes calibres.
    /// </summary>
    private static void ExibirTabelaComparativaPorMunicao()
    {
        var munitionKeys = Municoes.Keys.ToList();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n\n" + new string('#', 180));
        Console.WriteLine($"# TESTE DE DOSAGEM COMPARATIVA (EPICENTRO - {Municoes["81mm"].Calibre} = Referência)");
        Console.WriteLine(new string('#', 180));
        Console.ResetColor();

        // Cabeçalho Dinâmico
        string header = $"{"ALVO",-25}{"VIDA MAX (PV)",-15}";
        foreach (var key in munitionKeys)
        {
            header += $"{Municoes[key].Calibre,-20}";
        }
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(header);
        Console.WriteLine(new string('=', header.Length + (munitionKeys.Count * 5))); 
        Console.ResetColor();

        foreach (var parAlvo in AlvosBase.OrderBy(a => a.Value.VidaMaxima))
        {
            var alvoBase = parAlvo.Value;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{alvoBase.Nome,-25}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{alvoBase.VidaMaxima,-15:F0}");
            
            foreach (var key in munitionKeys)
            {
                var municao = Municoes[key];
                int tirosNecessarios = SimularTirosParaDestruicao(alvoBase, municao);
                
                // Cores Limpas para Identificação de Calibre
                ConsoleColor cor = ConsoleColor.Gray;
                if (key == "81mm") cor = ConsoleColor.Cyan;
                else if (key == "155mm") cor = ConsoleColor.Magenta;
                else if (key == "HeavyRocket") cor = ConsoleColor.Red;
                else if (key == "60mm") cor = ConsoleColor.DarkYellow;

                Console.ForegroundColor = cor;
                Console.Write($"{tirosNecessarios,-20}");
            }
            Console.WriteLine();
        }
        Console.ResetColor();
        Console.WriteLine(new string('=', header.Length + (munitionKeys.Count * 5)));
        Console.WriteLine($"Nota: Todos os valores representam quantos tiros (inteiros) de cada calibre são necessários para zerar a vida do alvo no ponto de impacto (Epicentro).");
    }
    
    /// <summary>
    /// Exibe a tabela de validação comparando a dosagem simulada do modelo com a referência TMQ.
    /// </summary>
    private static void ExibirTabelaValidacaoFinal()
    {
        var municao155 = Municoes["155mm"];
        var municao105 = Municoes["105/120mm"];
        var municao81 = Municoes["81mm"];
        
        // Dados de referência TMQ (extraídos da sua tabela)
        var refTMQ = new Dictionary<string, (int T155, int T105, int T81)>
        {
            {"Seç AAAe", (2, 6, 12)}, {"Bia AP", (2, 6, 12)}, {"Bia AR", (2, 6, 12)},
            {"Pos Metralhadora", (2, 6, 12)}, {"PO", (1, 3, 6)}, {"Pel Inf Mec", (2, 6, 12)},
            {"Pel CC", (2, 6, 12)}, {"AT 300X100", (2, 6, 12)}, {"PC 200X100", (1, 4, 8)},
            {"Pos Def – Esp Vtr", (2, 6, 12)}, {"Cln Vtr 250X100", (1, 4, 8)}, {"Pos Morteiro", (2, 6, 12)}
        };

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n\n" + new string('=', 180));
        Console.WriteLine("# TABELA FINAL DE VALIDAÇÃO: MODELO VS. REFERÊNCIA TMQ");
        Console.WriteLine(new string('=', 180));
        Console.ResetColor();
        
        // Cabeçalho da Tabela
        Console.WriteLine(string.Format("{0,-25}| {1,-20}| {2,-20}| {3,-20}| {4,-20}| {5,-20}| {6,-20}", 
            "ALVO", 
            "REF 155MM (TMQ)", 
            "MOD 155MM (SIM)", 
            "REF 105MM (TMQ)", 
            "MOD 105MM (SIM)", 
            "REF 81MM (TMQ)", 
            "MOD 81MM (SIM)"));
        Console.WriteLine(new string('-', 146));

        foreach (var parAlvo in AlvosBase.OrderBy(a => a.Value.VidaMaxima))
        {
            var nome = parAlvo.Key;
            var alvoBase = parAlvo.Value;
            var refData = refTMQ[nome];

            // Simulações
            int sim155 = SimularTirosParaDestruicao(alvoBase, municao155);
            int sim105 = SimularTirosParaDestruicao(alvoBase, municao105);
            int sim81 = SimularTirosParaDestruicao(alvoBase, municao81);
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(string.Format("{0,-25}| ", nome));
            
            // Coluna 155MM
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(string.Format("{0,-20}", refData.T155));
            Console.ForegroundColor = sim155 == refData.T155 ? ConsoleColor.Green : ConsoleColor.Red; // Verde/Vermelho
            Console.Write(string.Format("{0,-20}| ", sim155));

            // Coluna 105MM
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(string.Format("{0,-20}", refData.T105));
            Console.ForegroundColor = sim105 == refData.T105 ? ConsoleColor.Green : ConsoleColor.Red; // Verde/Vermelho
            Console.Write(string.Format("{0,-20}| ", sim105));

            // Coluna 81MM
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(string.Format("{0,-20}", refData.T81));
            Console.ForegroundColor = sim81 == refData.T81 ? ConsoleColor.Green : ConsoleColor.Red; // Verde/Vermelho
            Console.Write(string.Format("{0,-20}", sim81));

            Console.WriteLine();
        }
        Console.WriteLine(new string('=', 180));
        Console.WriteLine("LEGENDA:");
        Console.WriteLine("- VERDE        = A dosagem simulada é IDÊNTICA à referência TMQ.");
        Console.WriteLine("- VERMELHO     = A dosagem simulada é DIFERENTE da referência TMQ (Ajuste ou calibração de dano necessária).");
    }

    /// <summary>
    /// Exibe a tabela de análise de sensibilidade da probabilidade de acerto (Pk) e dosagem (Tiros) 
    /// em função da distância do impacto.
    /// </summary>
    private static void ExibirTabelaSensibilidade(string nomeAlvoReferencia)
    {
        var alvoRef = AlvosBase[nomeAlvoReferencia];
        var munitionKeys = new string[] { "155mm", "105/120mm", "81mm", "HeavyRocket" };
        var distancias = new double[] { 0, 5, 10, 15, 20 };

        Console.ForegroundColor = ConsoleColor.DarkGray; // Cor de fundo/base mais escura
        Console.WriteLine("\n\n" + new string('█', 120));
        Console.WriteLine($"# ANÁLISE DE SENSIBILIDADE: {alvoRef.Nome} (Vida: {alvoRef.VidaMaxima:F0}) vs. DISTÂNCIA (d)");
        Console.WriteLine(new string('█', 120));
        Console.ResetColor();

        // Cabeçalho (Usando Cores para Munições)
        Console.Write(string.Format("{0,-10}", "DIST. (m)"));
        foreach (var key in munitionKeys)
        {
            ConsoleColor corCalibre = ConsoleColor.White;
            if (key == "155mm") corCalibre = ConsoleColor.Magenta;
            else if (key == "105/120mm") corCalibre = ConsoleColor.Yellow;
            else if (key == "81mm") corCalibre = ConsoleColor.Blue;
            else if (key == "HeavyRocket") corCalibre = ConsoleColor.Red;

            Console.ForegroundColor = corCalibre;
            Console.Write(string.Format("{0,-25}", Municoes[key].Calibre));
        }
        Console.WriteLine();
        Console.WriteLine(new string('-', 120));
        Console.ResetColor();

        foreach (var d in distancias)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(string.Format("{0,-10:F0}", d)); // Distância (Col 1)

            foreach (var key in munitionKeys)
            {
                var municao = Municoes[key];
                
                // 1. Simular Pk
                double pk = SimulacaoDano.CalcularPk(d, municao.RaioDetonacaoM, alvoRef.Exposicao);
                
                // 2. Calcular Atenuação e Tiros Necessários
                double fatorAtenuacaoDano = 0;
                if (municao.RaioDetonacaoM > 0)
                {
                    fatorAtenuacaoDano = 1.0 - Math.Pow((d / municao.RaioDetonacaoM), 2);
                    fatorAtenuacaoDano = Math.Max(0, fatorAtenuacaoDano);
                }

                double tirosEpicentro = SimularTirosParaDestruicao(alvoRef, municao);
                double tirosNecessarios = 0;

                if (fatorAtenuacaoDano > 0.05) 
                {
                    tirosNecessarios = Math.Ceiling(tirosEpicentro / fatorAtenuacaoDano);
                } 
                else if (d < municao.RaioDetonacaoM)
                {
                    tirosNecessarios = 999; // Simula um valor muito alto (quase impossível de destruir)
                }
                else
                {
                    tirosNecessarios = 0; // Se Pk=0 e fora do raio
                }

                // Formatação da Célula: Pk% (Tiros)
                string display;
                
                if (tirosNecessarios == 0 && d < municao.RaioDetonacaoM)
                {
                    // Caso de Dano Zero por Fator Atenuação (Dano insignificante)
                    display = "Pk: " + $"{pk:F1}%";
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                }
                else if (tirosNecessarios == 0 || d >= municao.RaioDetonacaoM)
                {
                    display = "FORA DO RAIO";
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else if (tirosNecessarios >= 99)
                {
                    display = $"{pk:F1}% (>99 Tiros)";
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else
                {
                    display = $"{pk:F1}% ({tirosNecessarios:F0} Tiros)";
                    if (tirosNecessarios <= tirosEpicentro * 1.5) Console.ForegroundColor = ConsoleColor.Green; // Acerto de alta eficiência
                    else if (tirosNecessarios <= tirosEpicentro * 3) Console.ForegroundColor = ConsoleColor.DarkGreen; // Acerto de eficiência média
                    else Console.ForegroundColor = ConsoleColor.Yellow; // Acerto de baixa eficiência
                }
                
                Console.Write(string.Format("{0,-25}", display));
            }
            Console.WriteLine();
        }
        Console.WriteLine(new string('-', 120));
        Console.ResetColor();
        Console.WriteLine("--- NOTAS DE INTERPRETAÇÃO ---");
        Console.WriteLine("- Pk (%): Probabilidade de Acerto (LoS).");
        Console.WriteLine("- (Tiros): Número de tiros necessários para DESTRUIR o alvo a essa distância.");
        Console.WriteLine("- Verde: Alta eficiência. O custo (tiros) é baixo.");
        Console.WriteLine("- Amarelo: Baixa eficiência. O custo (tiros) aumenta rapidamente.");
        Console.WriteLine("- Vermelho: Fora do Raio ou custo proibitivo.");
    }
}
