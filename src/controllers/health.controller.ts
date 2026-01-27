import { Request, Response } from 'express';
import { cacheService } from '../services/cache.service';
import { HealthCheckResponse } from '../types';

export class HealthController {
  async check(req: Request, res: Response): Promise<void> {
    try {
      const stats = cacheService.getStats();
      const uptime = process.uptime();
      
      const response: HealthCheckResponse = {
        status: 'healthy',
        timestamp: new Date().toISOString(),
        uptime: Math.floor(uptime),
        cache: {
          status: 'active',
          keys: stats.keys
        }
      };
      
      res.status(200).json(response);
    } catch (error) {
      res.status(503).json({
        status: 'unhealthy',
        timestamp: new Date().toISOString(),
        error: 'Health check failed'
      });
    }
  }
}

export const healthController = new HealthController();
