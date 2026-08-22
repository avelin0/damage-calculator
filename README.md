






# Damage Calculator (Simulador de Dano de Artilharia)

## Screenshot 

<img width="1264" height="928" alt="image" src="https://github.com/user-attachments/assets/89b3ad02-debb-4584-aadc-67a1bd552d03" />

Simulador em C# (.NET) desenvolvido para modelar e analisar efeitos balísticos, probabilidade de acerto estocástica ($P_k$) e atenuação de dano por ogivas de artilharia e foguetes contra diferentes perfis de alvos táticos.

---

## 🎯 Sobre o Projeto

O sistema implementa estruturas lógicas para avaliar a eficácia e a dosagem de fogo (`TMQ` - Tabela de Munição e Quadro) de diversos calibres. Ele calcula a degradação dos Pontos de Vida (PV) dos alvos considerando:

* **Atenuação da explosão** baseada na distância do epicentro em relação ao raio de detonação ($R_{deton}$).
* **Probabilidade de acerto ($P_k$)** usando modelos de queda linear e referências analíticas avançadas (como o modelo exponencial de Carlton/Meyer).
* **Calibração de escala de dano** para compatibilizar os resultados teóricos com tabelas referenciais de instrução.

---

## 📐 O Modelo Matemático de Carlton (Meyer)

O projeto também integra referências a modelos analíticos avançados de eficácia de armas, como a **Função de Dano de Carlton**, implementada no código para cenários de dispersão elíptica:

```csharp
public static double CalcularDanoCarlton(double x, double y, double WRr, double WRd)
{
    double termoX = Math.Pow(x / WRr, 2);
    double termoY = Math.Pow(y / WRd, 2);
    double pkCarlton = Math.Exp(-(termoX + termoY));
    return Math.Clamp(pkCarlton, 0, 1);
}

```

### Características do Modelo:

* **Decaimento Exponencial com Distribuição Elíptica/Circular:** Utiliza uma função exponencial negativa baseada em uma distribuição gaussiana bidimensional, fazendo com que a probabilidade decresça de forma suave à medida que o impacto se afasta do centro.
* **Eixos de Alcance e Deflexão ($x$ e $y$):** Diferente de modelos puramente concêntricos, o modelo de Carlton separa o erro ou a distância nos eixos de **alcance** (`x`) e **deflexão** (`y`).
* **Raio de Arma (`WRr` e `WRd`):** Os parâmetros *Weapon Radius in Range* e *Weapon Radius in Deflection* funcionam como fatores de escala ou desvios padrão que definem a dispersão da ogiva, moldando a elípse de letalidade para alvos retangulares ou complexos.

---

## 🛠️ Tecnologias Utilizadas

* **Linguagem:** C# (.NET)
* **Paradigma:** Programação Orientada a Objetos (POO) / Lógica Estática Modular
* **Ferramentas de Análise:** Tabelas comparativas de dosagem, validação cruzada e análise de sensibilidade com saída colorida no console.

---

## 📊 Execução e Resultados da Simulação

Abaixo está a saída visual da ferramenta executada via terminal (`dotnet run`), demonstrando a validação cruzada de calibres e a matriz de sensibilidade espacial:

### O que o console exibe:

1. **Teste de Dosagem Comparativa (Epicentro):** Mostra a quantidade de tiros inteiros necessários para destruir diferentes alvos (como posições antiaéreas, baterias de artilharia e pelotões) utilizando calibres que vão de 60mm até Foguetes Pesados.
2. **Tabela Final de Validação (Modelo vs. Referência TMQ):** Compara a saída simulada pelo algoritmo com os dados de referência oficiais. As marcações em verde indicam alinhamento exato, enquanto as em vermelho apontam desvios que auxiliam na calibração fina de parâmetros.
3. **Análise de Sensibilidade (Distância vs. Eficiência):** Mapeia como a probabilidade de acerto ($P_k$) e o consumo de munição variam conforme o alvo se afasta gradualmente do ponto de impacto central ($0\text{m}$ a $20\text{m}$).

---

## 🚀 Como Executar o Projeto

Certifique-se de ter o [.NET SDK](https://www.google.com/search?q=https://dotnet.microsoft.com/) instalado em sua máquina.

1. Clone o repositório:
```bash
git clone https://github.com/avelin0/damage-calculator.git
cd damage-calculator

```


2. Execute o projeto:
```bash
dotnet run

```
