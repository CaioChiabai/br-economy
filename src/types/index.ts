export interface EconomicIndicator {
  date: string;
  value: number;
}

export interface IndicatorResponse {
  indicator: string;
  data: EconomicIndicator[];
  cached: boolean;
  lastUpdate: string;
}

export interface HealthCheckResponse {
  status: 'healthy' | 'degraded' | 'unhealthy';
  timestamp: string;
  uptime: number;
  cache: {
    status: 'active' | 'inactive';
    keys: number;
  };
}

export enum IndicatorType {
  SELIC = 'selic',
  IPCA = 'ipca',
  CDI = 'cdi',
  IGPM = 'igpm',
  DOLAR = 'dolar'
}

export interface IndicatorConfig {
  name: string;
  bcbCode?: number;
  ibgeCode?: string;
  cacheTTL: number;
  description: string;
}
