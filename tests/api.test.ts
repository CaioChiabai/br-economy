import request from 'supertest';
import app from '../src/app';

describe('API Health and Basic Routes', () => {
  describe('GET /health', () => {
    it('should return health status', async () => {
      const response = await request(app).get('/health');
      
      expect(response.status).toBe(200);
      expect(response.body).toHaveProperty('status', 'healthy');
      expect(response.body).toHaveProperty('timestamp');
      expect(response.body).toHaveProperty('uptime');
      expect(response.body).toHaveProperty('cache');
    });
  });

  describe('GET /', () => {
    it('should return API information', async () => {
      const response = await request(app).get('/');
      
      expect(response.status).toBe(200);
      expect(response.body).toHaveProperty('name');
      expect(response.body).toHaveProperty('version');
      expect(response.body).toHaveProperty('endpoints');
      expect(response.body).toHaveProperty('availableIndicators');
    });
  });

  describe('GET /api/indicators', () => {
    it('should list all available indicators', async () => {
      const response = await request(app).get('/api/indicators');
      
      expect(response.status).toBe(200);
      expect(response.body).toHaveProperty('indicators');
      expect(Array.isArray(response.body.indicators)).toBe(true);
      expect(response.body.indicators.length).toBeGreaterThan(0);
    });
  });

  describe('Error handling', () => {
    it('should return 404 for non-existent routes', async () => {
      const response = await request(app).get('/non-existent-route');
      
      expect(response.status).toBe(404);
      expect(response.body).toHaveProperty('status', 'error');
    });

    it('should return 400 for invalid indicator type', async () => {
      const response = await request(app).get('/api/indicators/invalid-type');
      
      expect(response.status).toBe(400);
      expect(response.body).toHaveProperty('status', 'error');
    });
  });
});
