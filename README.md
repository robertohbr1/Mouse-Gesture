# MouseKeyb

**MouseKeyb** é um simulador global de atalhos de teclado via gestos de mouse para Windows, desenvolvido em C# com WPF e .NET 10. 

Ao segurar o **botão direito do mouse** e desenhar um gesto na tela (como arrastar para baixo), o sistema intercepta o movimento, renderiza um rastro brilhante neon e simula um atalho de teclado correspondente (ex: `<Ctrl>W` para fechar abas do Chrome ou fechar o programa ativo).

---

## 🚀 Recursos principais

- **Menu Circular de Atalhos Rápidos**: Atalho global (`Ctrl` + `Botão Direito do Mouse`) que exibe um menu circular com 6 botões customizáveis em cima de uma imagem de blocos Lego, incluindo botões centrais de acesso rápido para **Lista**, **Configuração** e **Saída**.
- **Visualização e Execução por Lista (`CommandListWindow`)**: Um botão central **Lista** abre um painel modal moderno e escuro com a listagem de todos os comandos e gestos configurados. A lista é totalmente interativa e clicável: selecionar qualquer item executa instantaneamente o atalho de teclado correspondente. O modal também suporta fechamento rápido pressionando a tecla `Esc`.
- **Centralização Automática do Cursor**: Posiciona o ponteiro do mouse exatamente no centro do menu circular assim que ele é aberto, otimizando a velocidade e precisão de seleção dos comandos.
- **Configuração Dinâmica de Botões**: Clique com o **botão direito** em qualquer um dos 6 botões circulares para abrir um modal de configuração e definir o texto do botão (ajustado via `Viewbox`) e o comando associado (com suporte a argumentos/parâmetros de linha de comando).
- **Execução Automática**: Clique com o **botão esquerdo** no botão configurado para iniciar o programa correspondente instantaneamente com seus parâmetros associados.
- **Gestos de Múltiplos Traços**: Combinações de direções (Cima `U`, Baixo `D`, Esquerda `L`, Direita `R`), permitindo gestos complexos como `DR` (Baixo-Direita) ou `UL` (Cima-Esquerda).
- **Rastro Visual Neon & Realce de Direção**: Desenha uma linha brilhante ciano semi-transparente que acompanha o cursor e destaca com a cor verde a ação correspondente quando um movimento de liberação é detectado.
- **Não Rouba Foco**: A tela de desenho usa o estilo estendido `WS_EX_NOACTIVATE` do Windows, garantindo que o foco continue no programa ativo.
- **Gravador de Atalhos com Hook Global**: Painel administrativo escuro e moderno que utiliza um hook de teclado de baixo nível (`WH_KEYBOARD_LL`) temporário para capturar atalhos de teclado de forma isolada, limpa e segura (sem disparar ações em outros programas).
- **Validação de Gestos Únicos**: Previne a criação de gestos duplicados por engano, exibindo um alerta visual e revertendo automaticamente para o padrão original ou seguro, além de normalizar letras para maiúsculas e remover espaços extras.
- **Execução em Segundo Plano**: Minimiza-se nativamente para a bandeja do sistema (System Tray).
- **Instância Única (Single Instance)**: Proteção por `Mutex` que impede a execução de instâncias duplicadas e evita conflito de ganchos do Windows.
- **Inicialização Automática com o Windows**: Opção configurável diretamente na tela de configurações globais para iniciar o aplicativo automaticamente quando o Windows iniciar (gerenciada de forma segura através do registro do Windows).

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

## 🧪 Testes Unitários e Integração

O projeto possui uma suíte de testes robusta utilizando xUnit que cobre diversos componentes vitais:

- **Reconhecimento de Gestos (`GestureRecognizerTests.cs`)**: Valida o algoritmo de identificação de trajetórias e direções (Cima `U`, Baixo `D`, Esquerda `L`, Direita `R`) e as margens de tolerância de pixels (`SegmentThreshold`).
- **Análise de Teclas (`KeysParsingTests.cs`)**: Garante a tradução correta das strings de atalhos em eventos estruturados (como pressionar/soltar teclas de modificação).
- **Ciclo de Vida e Bandeja do Sistema (`TrayMenuTests.cs`)**: Testa a integração de janelas no WPF, comportamento de ocultar/minimizar janelas ao fechar e interação com itens do menu do System Tray.
- **Intercepção Global (`MouseHookTests.cs`)**: Verifica a ativação e comportamento do gancho global de mouse de baixo nível.
- **Inicialização do Sistema (`StartupServiceTests.cs`)**: Valida o registro e remoção das chaves de inicialização automática no registro do Windows de forma mockada.

Para rodar todos os testes automatizados da aplicação:
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
  "SegmentThreshold": 40.0,
  "StartWithWindows": false
}
```

---

## 📄 Licença

Este projeto é de código aberto sob a licença [MIT](LICENSE).
