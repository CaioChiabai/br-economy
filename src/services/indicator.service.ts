import axios, { AxiosInstance, AxiosRequestConfig } from 'axios';
import { config } from '../config';
import { EconomicIndicator, IndicatorType } from '../types';
import { cacheService } from './cache.service';

class IndicatorService {
  private bcbClient: AxiosInstance;
  
  // BCB (Banco Central do Brasil) codes for indicators
  private readonly indicatorCodes = {
    [IndicatorType.SELIC]: 432,    // Taxa Selic
    [IndicatorType.IPCA]: 433,     // IPCA
    [IndicatorType.CDI]: 12,       // CDI
    [IndicatorType.IGPM]: 189,     // IGP-M
    [IndicatorType.DOLAR]: 1,      // Dólar
  };
  
  private readonly cacheTTL = {
    [IndicatorType.SELIC]: config.cache.ttl.selic,
    [IndicatorType.IPCA]: config.cache.ttl.ipca,
    [IndicatorType.CDI]: config.cache.ttl.cdi,
    [IndicatorType.IGPM]: config.cache.ttl.igpm,
    [IndicatorType.DOLAR]: config.cache.ttl.dolar,
  };
  
  constructor() {
    this.bcbClient = axios.create({
      baseURL: config.externalApi.bcbUrl,
      timeout: config.externalApi.timeout,
      headers: {
        'Accept': 'application/json',
      }
    });
  }
  
  async getIndicator(
    type: IndicatorType,
    startDate?: string,
    endDate?: string
  ): Promise<{ data: EconomicIndicator[], cached: boolean }> {
    const cacheKey = `${type}_${startDate || 'all'}_${endDate || 'latest'}`;
    
    // Check cache first
    const cachedData = cacheService.get(cacheKey);
    if (cachedData) {
      return { data: cachedData, cached: true };
    }
    
    // Fetch from BCB API with retry logic
    const data = await this.fetchWithRetry(type, startDate, endDate);
    
    // Store in cache
    cacheService.set(cacheKey, data, this.cacheTTL[type]);
    
    return { data, cached: false };
  }
  
  private async fetchWithRetry(
    type: IndicatorType,
    startDate?: string,
    endDate?: string,
    retryCount = 0
  ): Promise<EconomicIndicator[]> {
    try {
      const code = this.indicatorCodes[type];
      const url = `/${code}/dados`;
      
      const params: any = {
        formato: 'json'
      };
      
      if (startDate) {
        params.dataInicial = startDate;
      }
      if (endDate) {
        params.dataFinal = endDate;
      }
      
      const response = await this.bcbClient.get(url, { params });
      
      // Transform BCB response to our format
      return this.transformBCBResponse(response.data);
    } catch (error: any) {
      if (retryCount < config.externalApi.maxRetries) {
        // Exponential backoff
        const delay = Math.pow(2, retryCount) * 1000;
        await new Promise(resolve => setTimeout(resolve, delay));
        return this.fetchWithRetry(type, startDate, endDate, retryCount + 1);
      }
      
      throw new Error(`Failed to fetch ${type} data after ${config.externalApi.maxRetries} retries: ${error.message}`);
    }
  }
  
  private transformBCBResponse(data: any[]): EconomicIndicator[] {
    if (!Array.isArray(data)) {
      return [];
    }
    
    return data.map(item => ({
      date: item.data,
      value: parseFloat(item.valor)
    }));
  }
  
  async getLatest(type: IndicatorType): Promise<EconomicIndicator | null> {
    const cacheKey = `${type}_latest`;
    
    // Check cache
    const cachedData = cacheService.get(cacheKey);
    if (cachedData) {
      return cachedData;
    }
    
    try {
      const code = this.indicatorCodes[type];
      const url = `/${code}/dados/ultimos/1`;
      
      const response = await this.bcbClient.get(url, {
        params: { formato: 'json' }
      });
      
      const transformed = this.transformBCBResponse(response.data);
      const latest = transformed[0] || null;
      
      if (latest) {
        cacheService.set(cacheKey, latest, this.cacheTTL[type]);
      }
      
      return latest;
    } catch (error: any) {
      throw new Error(`Failed to fetch latest ${type}: ${error.message}`);
    }
  }
}

export const indicatorService = new IndicatorService();
