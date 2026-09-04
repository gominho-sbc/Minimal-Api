# Guia Completo do MySQL no Windows: Da Instalação às Consultas Avançadas

Este guia prático foi desenvolvido para auxiliar na instalação, configuração e utilização do ecossistema MySQL no sistema operacional Microsoft Windows, cobrindo desde os fundamentos até conceitos avançados de bancos de dados relacionais.

---

## 1. Passo a Passo da Instalação

### Passo 1: Download do Instalador
1. Acesse o site oficial do MySQL em: [Download MySQL Installer](https://dev.mysql.com/downloads/installer/).
2. Certifique-se de selecionar a plataforma **Microsoft Windows**.
3. Escolha a versão **completa offline** (indicada por `mysql-installer-community-...`).
4. Na tela de login da Oracle, você não precisa criar uma conta. Basta clicar no link inferior: **"No thanks, just start my download"**.

### Passo 2: Execução e Escolha de Componentes
1. Execute o arquivo `.msi` baixado e conceda permissões de administrador.
2. Na tela de tipo de instalação, selecione **Developer Default** (instala a maioria dos recursos necessários) ou **Full**.
3. Siga clicando em **Next** e depois em **Execute** para instalar os pacotes em segundo plano.

### Passo 3: Configuração do Servidor
1. **Tipo de Rede (Type and Networking):** Mantenha o padrão *Development Computer* e a porta **3306** configurada.
2. **Método de Autenticação:** Escolha o recomendado (*Strong Password Authentication*).
3. **Contas de Usuário:** Defina uma senha forte para o usuário administrador padrão (**root**). Anote esta senha, pois ela é necessária para qualquer conexão posterior.
4. **Serviço do Windows (Windows Service):** Marque para iniciar o MySQL Server junto com a inicialização do Windows automaticamente.
5. Avance e clique em **Execute** para aplicar as configurações. Clique em **Finish** ao finalizar.

---

## 2. Configurando as Variáveis de Ambiente (Path)

Para executar o MySQL de qualquer terminal (Prompt de Comando ou PowerShell) sem precisar digitar o caminho completo da pasta, configure a variável de ambiente:

1. Abra o Windows Explorer e localize a pasta de instalação do seu servidor MySQL (O padrão geralmente é: `C:\Program Files\MySQL\MySQL Server 8.0\bin` ou versão equivalente). Copie este caminho de diretório.
2. No menu iniciar do Windows, pesquise por **"Editar as variáveis de ambiente do sistema"** e abra-o.
3. Clique no botão **Variáveis de Ambiente...** na parte inferior da aba Avançado.
4. Em *Variáveis do Sistema*, encontre a linha chamada **Path** e clique duas vezes sobre ela.
5. Clique no botão **Novo** e cole o caminho copiado no item 1.
6. Clique em **OK** em todas as janelas abertas para salvar.
7. Abra um novo terminal e teste digitando: `mysql -u root -p` (insira a senha do root configurada para logar).

---

## 3. Resumo dos Componentes Principais

| Componente | Descrição Prática |
| :--- | :--- |
| **MySQL Server** | O motor principal do banco de dados que executa os serviços, processa queries e armazena os arquivos de dados locais. |
| **MySQL Workbench** | Interface gráfica integrada para gerenciamento visual, modelagem de tabelas, execução de scripts SQL e monitoramento de desempenho. |
| **MySQL Shell** | Terminal de linha de comando avançado que suporta interações via JavaScript, Python e código SQL padrão. |
| **MySQL Connectors** | Drivers de comunicação essenciais para conectar linguagens de programação (Python, Java, PHP, C#) ao banco de dados MySQL. |

---

## 4. Primeiros Passos e Comandos CRUD Básicos

### Gerenciando Bancos de Dados
```sql
-- Criar um novo banco de dados
CREATE DATABASE e_commerce;

-- Listar bancos existentes no servidor
SHOW DATABASES;

-- Indicar ao terminal qual banco de dados usar para os próximos comandos
USE e_commerce;
```

### Criando Tabelas Simples
```sql
-- Criação de uma tabela de clientes com ID autoincrementável e chave primária
CREATE TABLE clientes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE,
    data_cadastro DATE
);

-- Exibir a estrutura física e tipos de dados da tabela criada
DESCRIBE clientes;
```

### Operações CRUD (Create, Read, Update, Delete)
```sql
-- C (Create): Inserir dados nas tabelas
INSERT INTO clientes (nome, email, data_cadastro) 
VALUES ('Ana Silva', 'ana.silva@email.com', '2026-01-15');

INSERT INTO clientes (nome, email, data_cadastro) 
VALUES ('Carlos Souza', 'carlos.souza@email.com', '2026-02-20');

-- R (Read): Consultar dados estruturados
SELECT * FROM clientes;
SELECT nome, email FROM clientes WHERE id = 1;

-- U (Update): Alterar registros existentes (ATENÇÃO: Use sempre o WHERE!)
UPDATE clientes 
SET email = 'ana.nova@email.com' 
WHERE id = 1;

-- D (Delete): Remover registros específicos (ATENÇÃO: Use sempre o WHERE!)
DELETE FROM clientes 
WHERE id = 2;
```

---

## 5. Modelagem Avançada: Relacionamentos e Chaves Estrangeiras

Em bancos de dados relacionais, tabelas conversam entre si por meio de **Chaves Estrangeiras (Foreign Keys)**, garantindo a integridade referencial dos dados.

### Criando Tabela Relacionada (1 para Muitos)
Criaremos uma tabela de `pedidos` que depende e se conecta diretamente à tabela de `clientes` usando a coluna `cliente_id`.

```sql
CREATE TABLE pedidos (
    pedido_id INT AUTO_INCREMENT PRIMARY KEY,
    data_pedido DATETIME DEFAULT CURRENT_TIMESTAMP,
    valor_total DECIMAL(10, 2) NOT NULL,
    cliente_id INT,
    -- Definição do vínculo e restrição de integridade
    FOREIGN KEY (cliente_id) REFERENCES clientes(id)
    ON DELETE CASCADE
);
```
> **Nota de Arquitetura:** O parâmetro `ON DELETE CASCADE` garante que, se um cliente for deletado, todos os pedidos vinculados a ele sejam removidos automaticamente do sistema, evitando registros órfãos.

---

## 6. Consultas Avançadas com Cláusula JOIN

O comando `JOIN` serve para unificar e combinar dados distribuídos em duas ou mais tabelas em um único resultado de consulta estruturado.

### Inserindo Dados para Teste de Relacionamento
```sql
-- Primeiro, garantimos que temos um cliente ativo
INSERT INTO clientes (nome, email, data_cadastro) 
VALUES ('Roberto Oliveira', 'roberto@email.com', '2026-03-01');

-- Inserimos pedidos amarrados ao id do cliente correspondente
INSERT INTO pedidos (valor_total, cliente_id) VALUES (250.50, 3);
INSERT INTO pedidos (valor_total, cliente_id) VALUES (89.90, 3);
```

### INNER JOIN (Intersecção Exata)
Retorna apenas os registros que possuem correspondência exata em ambas as tabelas (ou seja, clientes que possuem pedidos cadastrados).

```sql
SELECT 
    clientes.nome, 
    clientes.email, 
    pedidos.pedido_id, 
    pedidos.valor_total, 
    pedidos.data_pedido
FROM clientes
INNER JOIN pedidos ON clientes.id = pedidos.cliente_id;
```

### LEFT JOIN (Inclusão à Esquerda)
Retorna todos os clientes cadastrados da tabela à esquerda (`clientes`), independentemente de terem feito pedidos ou não. Caso não tenham feito, as colunas da tabela `pedidos` retornarão como `NULL`.

```sql
SELECT 
    clientes.nome, 
    pedidos.pedido_id, 
    pedidos.valor_total
FROM clientes
LEFT JOIN pedidos ON clientes.id = pedidos.cliente_id;
```

### Agrupamento Avançado (GROUP BY com JOIN)
Consulta que calcula e agrupa o valor total gasto por cada cliente individual no sistema:

```sql
SELECT 
    clientes.nome, 
    COUNT(pedidos.pedido_id) AS total_de_pedidos,
    SUM(pedidos.valor_total) AS total_gasto_acumulado
FROM clientes
INNER JOIN pedidos ON clientes.id = pedidos.cliente_id
GROUP BY clientes.id, clientes.nome
ORDER BY total_gasto_acumulado DESC;
```