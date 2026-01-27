import NodeCache from 'node-cache';
import { config } from '../config';

class CacheService {
  private cache: NodeCache;
  
  constructor() {
    this.cache = new NodeCache({ 
      stdTTL: 3600,
      checkperiod: 600,
      useClones: true  // Clone objects to prevent cache pollution
    });
  }
  
  get(key: string): any {
    return this.cache.get(key);
  }
  
  set(key: string, value: any, ttl?: number): boolean {
    return this.cache.set(key, value, ttl || 3600);
  }
  
  has(key: string): boolean {
    return this.cache.has(key);
  }
  
  del(key: string): number {
    return this.cache.del(key);
  }
  
  flush(): void {
    this.cache.flushAll();
  }
  
  getStats() {
    return this.cache.getStats();
  }
  
  keys(): string[] {
    return this.cache.keys();
  }
}

export const cacheService = new CacheService();
