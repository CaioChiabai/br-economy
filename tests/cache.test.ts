import { cacheService } from '../src/services/cache.service';

describe('CacheService', () => {
  beforeEach(() => {
    cacheService.flush();
  });

  describe('set and get', () => {
    it('should store and retrieve data', () => {
      const key = 'test-key';
      const value = { test: 'data' };
      
      cacheService.set(key, value);
      const retrieved = cacheService.get(key);
      
      expect(retrieved).toEqual(value);
    });

    it('should return undefined for non-existent keys', () => {
      const retrieved = cacheService.get('non-existent');
      expect(retrieved).toBeUndefined();
    });
  });

  describe('has', () => {
    it('should return true for existing keys', () => {
      const key = 'test-key';
      cacheService.set(key, 'value');
      
      expect(cacheService.has(key)).toBe(true);
    });

    it('should return false for non-existent keys', () => {
      expect(cacheService.has('non-existent')).toBe(false);
    });
  });

  describe('del', () => {
    it('should delete a key', () => {
      const key = 'test-key';
      cacheService.set(key, 'value');
      
      expect(cacheService.has(key)).toBe(true);
      cacheService.del(key);
      expect(cacheService.has(key)).toBe(false);
    });
  });

  describe('flush', () => {
    it('should clear all keys', () => {
      cacheService.set('key1', 'value1');
      cacheService.set('key2', 'value2');
      
      expect(cacheService.keys().length).toBe(2);
      cacheService.flush();
      expect(cacheService.keys().length).toBe(0);
    });
  });

  describe('getStats', () => {
    it('should return cache statistics', () => {
      cacheService.set('key1', 'value1');
      const stats = cacheService.getStats();
      
      expect(stats).toHaveProperty('keys');
      expect(stats.keys).toBeGreaterThanOrEqual(1);
    });
  });
});
