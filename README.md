# MouseKeyb

**MouseKeyb** é um simulador global de atalhos de teclado via gestos de mouse para Windows, desenvolvido em C# com WPF e .NET 10. 

Ao segurar o **botão direito do mouse** e desenhar um gesto na tela (como arrastar para baixo), o sistema intercepta o movimento, renderiza um rastro brilhante neon e simula um atalho de teclado correspondente (ex: `<Ctrl>W` para fechar abas do Chrome ou fechar o programa ativo).

---

## 🚀 Recursos principais

- **Menu Circular de Atalhos Rápidos**: Atalho global (`Ctrl` + `Botão Direito do Mouse`) que exibe um menu de layout circular com 6 botões customizáveis em cima de uma imagem de anel de blocos lego.
- **Configuração Dinâmica de Botões**: Clique com o **botão direito** em qualquer um dos 6 botões circulares para abrir um modal de configuração e definir o texto do botão (que se ajusta automaticamente ao espaço usando `Viewbox`) e o caminho do programa executável associado.
- **Execução Automática**: Clique com o **botão esquerdo** em qualquer botão configurado para iniciar o programa correspondente de forma instantânea (com fallback seguro de Notepad++ para Bloco de Notas).
- **Gestos de Múltiplos Traços**: Suporta combinações de direções (Cima `U`, Baixo `D`, Esquerda `L`, Direita `R`), permitindo gestos como `DR` (Baixo depois Direita) ou `UL` (Cima depois Esquerda).
- **Rastro Visual Neon**: Desenha uma linha brilhante ciano anti-aliasing semi-transparente que acompanha o cursor e desaparece com uma animação suave de *fade-out* ao soltar o botão.
- **Não Rouba Foco**: A tela de desenho de gestos usa o estilo estendido `WS_EX_NOACTIVATE` do Windows, garantindo que o foco nunca seja retirado do programa ativo.
- **Gravador de Atalhos Integrado**: Painel administrativo escuro e moderno para cadastrar ações, atribuir gestos e capturar atalhos físicos pressionados no teclado.
- **Execução em Segundo Plano**: O aplicativo se minimiza nativamente para a bandeja do sistema (System Tray).
- **Instância Única (Single Instance)**: Proteção por `Mutex` que impede a execução de instâncias duplicadas e evita conflito de hooks.

---

## 🛠️ Tecnologia e Arquitetura

- **Interface**: WPF (Windows Presentation Foundation) com design escuro personalizado.
- **Integração com Windows API**:
  - `SetWindowsHookEx` (gancho global `WH_MOUSE_LL` para detectar cliques e arrastos fora da janela do app).
  - `SendInput` (simulação de teclado compatível com 64 bits de forma nativa).
  - `MapVirtualKey` (mapeamento de teclas virtuais para scan codes de teclado físico, aumentando a compatibilidade com navegadores).

---

## 📋 Pré-requisitos

- **Sistema Operacional**: Windows 10 / 11 (64 bits recomendado).
- **Ambiente de desenvolvimento**: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) instalado.

---

## ⚙️ Instalação e Execução

### Compilar e Rodar o Projeto
Abra o prompt de comando (ou PowerShell) no diretório do projeto e execute:
```powershell
dotnet run --project MouseKeyb.csproj
```

### Compilação de Produção (Release)
Para compilar a versão otimizada de alta performance:
```powershell
dotnet build -c Release
```
O executável final estará disponível em:
`bin/Release/net10.0-windows/MouseKeyb.exe`

### Criar Atalho no Desktop
Você pode gerar o atalho no Desktop automaticamente rodando este script rápido no PowerShell:
```powershell
$desktop = [System.Environment]::GetFolderPath('Desktop')
$target = "C:\Projetos\MouseKeyb\bin\Release\net10.0-windows\MouseKeyb.exe"
$workingDir = "C:\Projetos\MouseKeyb\bin\Release\net10.0-windows"
$shortcutPath = [System.IO.Path]::Combine($desktop, "MouseKeyb.lnk")
$wshell = New-Object -ComObject WScript.Shell
$shortcut = $wshell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $target
$shortcut.WorkingDirectory = $workingDir
$shortcut.Save()
```

---

## 🧪 Testes Unitários

O projeto conta com testes de cobertura para o motor do reconhecedor de gestos (`GestureRecognizer`). Para executá-los:
```powershell
dotnet test MouseKeyb.Tests/MouseKeyb.Tests.csproj
```

---

## 🎮 Gestos Padrão Configurados

O sistema já vem pré-configurado com uma série de gestos e atalhos úteis:

| Nome do Comando (Programa) | Movimento (Gesto) | Combinação de Teclas | Descrição do Atalho |
| :--- | :---: | :--- | :--- |
| **Browser - Fechar Aba** | `D` *(Down)* | `<Ctrl>+W` | Fecha a aba ativa do navegador |
| **Browser - Voltar** | `L` *(Left)* | `<Alt>+Left` | Volta à página anterior |
| **Browser - Avançar** | `R` *(Right)* | `<Alt>+Right` | Avança para a próxima página |
| **Browser - Nova Aba** | `U` *(Up)* | `<Ctrl>+T` | Abre uma nova aba no navegador |
| **Browser - Reabrir Aba Fechada** | `UD` *(Up-Down)* | `<Ctrl>+<Shift>+T` | Reabre a última aba que foi fechada |
| **Browser - Próxima Aba** | `DR` *(Down-Right)* | `<Ctrl>+Tab` | Alterna para a próxima aba à direita |
| **Browser - Aba Anterior** | `DL` *(Down-Left)* | `<Ctrl>+<Shift>+Tab` | Alterna para a aba à esquerda |
| **Browser - Recarregar Página** | `RU` *(Right-Up)* | `F5` | Recarrega a página atual |
| **Windows - Fechar Janela** | `UL` *(Up-Left)* | `<Alt>+F4` | Fecha o aplicativo ou janela em foco |
| **Windows - Mostrar Área de Trabalho** | `LU` *(Left-Up)* | `<Win>+D` | Minimiza todas as janelas para ver o desktop |
| **Windows - Alternar Janelas** | `LD` *(Left-Down)* | `<Win>+Tab` | Abre o Task View do Windows |

---

## 📂 Estrutura de Arquivos de Configuração

As configurações e mapeamentos são serializados em formato JSON e salvos na pasta de dados locais do usuário:
`%APPDATA%\MouseKeyb\settings.json`

**Exemplo de Configuração:**
```json
{
  "Mappings": [
    {
      "Pattern": "D",
      "ActionName": "Browser - Fechar Aba",
      "Keys": [
        { "Vk": 162, "Name": "Ctrl", "Type": "Down" },
        { "Vk": 87, "Name": "W", "Type": "Press" },
        { "Vk": 162, "Name": "Ctrl", "Type": "Up" }
      ]
    }
  ],
  "SegmentThreshold": 40.0
}
```

---

## 📄 Licença

Este projeto é de código aberto sob a licença [MIT](LICENSE).
