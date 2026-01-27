import dotenv from 'dotenv';

dotenv.config();

export const config = {
  port: parseInt(process.env.PORT || '3000', 10),
  nodeEnv: process.env.NODE_ENV || 'development',
  
  cache: {
    ttl: {
      selic: parseInt(process.env.CACHE_TTL_SELIC || '3600', 10),
      ipca: parseInt(process.env.CACHE_TTL_IPCA || '3600', 10),
      cdi: parseInt(process.env.CACHE_TTL_CDI || '3600', 10),
      igpm: parseInt(process.env.CACHE_TTL_IGPM || '3600', 10),
      dolar: parseInt(process.env.CACHE_TTL_DOLAR || '300', 10),
    }
  },
  
  rateLimit: {
    windowMs: parseInt(process.env.RATE_LIMIT_WINDOW_MS || '60000', 10),
    maxRequests: parseInt(process.env.RATE_LIMIT_MAX_REQUESTS || '100', 10),
  },
  
  externalApi: {
    bcbUrl: process.env.BCB_API_URL || 'https://api.bcb.gov.br/dados/serie/bcdata.sgs',
    ibgeUrl: process.env.IBGE_API_URL || 'https://servicodados.ibge.gov.br/api/v3',
    timeout: parseInt(process.env.REQUEST_TIMEOUT || '10000', 10),
    maxRetries: parseInt(process.env.MAX_RETRIES || '3', 10),
  }
};
