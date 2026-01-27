# Brazilian Economic Indicators API 🇧🇷

Uma API de alta performance que atua como um **Hub Centralizado de Indicadores Econômicos do Brasil** (Selic, IPCA, CDI, etc.), projetada para resolver problemas comuns de integração com fontes governamentais como instabilidade, lentidão e limitação de requisições.

## 🎯 Características

- **Alta Performance**: Sistema de cache inteligente com TTL configurável por indicador
- **Resiliência**: Retry automático com backoff exponencial para lidar com instabilidades
- **Rate Limiting**: Proteção contra sobrecarga com controle de requisições
- **Fácil Integração**: API RESTful simples e bem documentada
- **Pronto para Produção**: Docker, health checks e tratamento robusto de erros

## 📊 Indicadores Suportados

| Indicador | Descrição | Cache TTL |
|-----------|-----------|-----------|
| **SELIC** | Taxa básica de juros | 1 hora |
| **IPCA** | Índice de Preços ao Consumidor | 1 hora |
| **CDI** | Certificado de Depósito Interbancário | 1 hora |
| **IGP-M** | Índice Geral de Preços do Mercado | 1 hora |
| **DÓLAR** | Taxa de câmbio USD/BRL | 5 minutos |

## 🚀 Quick Start

### Instalação

```bash
# Clone o repositório
git clone https://github.com/CaioChiabai/br-economy.git
cd br-economy

# Instale as dependências
npm install

# Configure as variáveis de ambiente
cp .env.example .env

# Inicie em modo desenvolvimento
npm run dev
```

### Usando Docker

```bash
# Build e execute com Docker Compose
docker-compose up -d

# Verifique os logs
docker-compose logs -f
```

## 📖 Uso da API

### Endpoints Disponíveis

#### 1. Listar Indicadores
```bash
GET /api/indicators
```

**Resposta:**
```json
{
  "indicators": [
    {
      "type": "selic",
      "name": "Taxa SELIC",
      "description": "Taxa básica de juros da economia brasileira"
    }
  ],
  "endpoints": {
    "list": "/api/indicators",
    "get": "/api/indicators/:type",
    "latest": "/api/indicators/:type/latest"
  }
}
```

#### 2. Obter Dados de um Indicador
```bash
GET /api/indicators/:type?startDate=DD/MM/YYYY&endDate=DD/MM/YYYY
```

**Exemplo:**
```bash
curl http://localhost:3000/api/indicators/selic?startDate=01/01/2024&endDate=31/12/2024
```

**Resposta:**
```json
{
  "indicator": "selic",
  "data": [
    {
      "date": "01/01/2024",
      "value": 11.75
    }
  ],
  "cached": true,
  "lastUpdate": "2024-01-27T21:30:00.000Z"
}
```

#### 3. Obter Último Valor
```bash
GET /api/indicators/:type/latest
```

**Exemplo:**
```bash
curl http://localhost:3000/api/indicators/dolar/latest
```

**Resposta:**
```json
{
  "indicator": "dolar",
  "data": {
    "date": "27/01/2024",
    "value": 4.87
  },
  "timestamp": "2024-01-27T21:30:00.000Z"
}
```

#### 4. Health Check
```bash
GET /health
```

**Resposta:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-27T21:30:00.000Z",
  "uptime": 3600,
  "cache": {
    "status": "active",
    "keys": 15
  }
}
```

## 🛠️ Desenvolvimento

### Scripts Disponíveis

```bash
# Desenvolvimento com hot-reload
npm run dev

# Build para produção
npm run build

# Executar produção
npm start

# Executar testes
npm test

# Testes com cobertura
npm run test:coverage

# Testes em modo watch
npm run test:watch
```

### Estrutura do Projeto

```
br-economy/
├── src/
│   ├── config/           # Configurações da aplicação
│   ├── controllers/      # Controllers da API
│   ├── middlewares/      # Middlewares Express
│   ├── services/         # Lógica de negócio e integrações
│   ├── types/           # Definições TypeScript
│   ├── app.ts           # Configuração do Express
│   └── server.ts        # Ponto de entrada
├── tests/               # Testes automatizados
├── .env.example         # Exemplo de variáveis de ambiente
├── Dockerfile           # Configuração Docker
├── docker-compose.yml   # Orquestração Docker
└── tsconfig.json        # Configuração TypeScript
```

## ⚙️ Configuração

### Variáveis de Ambiente

```env
# Servidor
PORT=3000
NODE_ENV=development

# Cache (em segundos)
CACHE_TTL_SELIC=3600
CACHE_TTL_IPCA=3600
CACHE_TTL_CDI=3600
CACHE_TTL_IGPM=3600
CACHE_TTL_DOLAR=300

# Rate Limiting
RATE_LIMIT_WINDOW_MS=60000
RATE_LIMIT_MAX_REQUESTS=100

# APIs Externas
REQUEST_TIMEOUT=10000
MAX_RETRIES=3
```

## 🏗️ Arquitetura

### Principais Componentes

1. **Cache Layer**: Sistema de cache em memória (node-cache) com TTL diferenciado por indicador
2. **Retry Mechanism**: Retry automático com backoff exponencial para lidar com falhas temporárias
3. **Rate Limiting**: Proteção contra abuso com express-rate-limit
4. **Error Handling**: Tratamento centralizado de erros com middlewares dedicados

### Fluxo de Requisição

```
Cliente → Rate Limiter → Cache Check → External API (com retry) → Cache Store → Response
```

## 🧪 Testes

```bash
# Executar todos os testes
npm test

# Cobertura de testes
npm run test:coverage

# Modo watch
npm run test:watch
```

## 📝 Licença

ISC

## 🤝 Contribuindo

Contribuições são bem-vindas! Por favor:

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/NovaFeature`)
3. Commit suas mudanças (`git commit -m 'Adiciona nova feature'`)
4. Push para a branch (`git push origin feature/NovaFeature`)
5. Abra um Pull Request

## 📞 Suporte

Para questões e suporte, abra uma issue no GitHub.

---

Desenvolvido com ❤️ para a comunidade brasileira de desenvolvedores
