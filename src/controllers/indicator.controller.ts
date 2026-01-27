import { Request, Response, NextFunction } from 'express';
import { indicatorService } from '../services/indicator.service';
import { IndicatorType, IndicatorResponse } from '../types';
import { AppError } from '../middlewares/error.middleware';

export class IndicatorController {
  async getIndicator(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const { type } = req.params;
      const { startDate, endDate } = req.query;
      
      // Validate indicator type
      if (!Object.values(IndicatorType).includes(type as IndicatorType)) {
        throw new AppError(`Invalid indicator type: ${type}`, 400);
      }
      
      const indicatorType = type as IndicatorType;
      
      const result = await indicatorService.getIndicator(
        indicatorType,
        startDate as string | undefined,
        endDate as string | undefined
      );
      
      const response: IndicatorResponse = {
        indicator: indicatorType,
        data: result.data,
        cached: result.cached,
        lastUpdate: new Date().toISOString()
      };
      
      res.status(200).json(response);
    } catch (error) {
      next(error);
    }
  }
  
  async getLatest(req: Request, res: Response, next: NextFunction): Promise<void> {
    try {
      const { type } = req.params;
      
      // Validate indicator type
      if (!Object.values(IndicatorType).includes(type as IndicatorType)) {
        throw new AppError(`Invalid indicator type: ${type}`, 400);
      }
      
      const data = await indicatorService.getLatest(type as IndicatorType);
      
      if (!data) {
        throw new AppError(`No data available for ${type}`, 404);
      }
      
      res.status(200).json({
        indicator: type,
        data,
        timestamp: new Date().toISOString()
      });
    } catch (error) {
      next(error);
    }
  }
  
  async listIndicators(req: Request, res: Response): Promise<void> {
    const indicators = [
      {
        type: IndicatorType.SELIC,
        name: 'Taxa SELIC',
        description: 'Taxa básica de juros da economia brasileira'
      },
      {
        type: IndicatorType.IPCA,
        name: 'IPCA',
        description: 'Índice Nacional de Preços ao Consumidor Amplo'
      },
      {
        type: IndicatorType.CDI,
        name: 'CDI',
        description: 'Certificado de Depósito Interbancário'
      },
      {
        type: IndicatorType.IGPM,
        name: 'IGP-M',
        description: 'Índice Geral de Preços do Mercado'
      },
      {
        type: IndicatorType.DOLAR,
        name: 'Dólar',
        description: 'Taxa de câmbio Dólar Americano/Real'
      }
    ];
    
    res.status(200).json({
      indicators,
      endpoints: {
        list: '/api/indicators',
        get: '/api/indicators/:type',
        latest: '/api/indicators/:type/latest'
      }
    });
  }
}

export const indicatorController = new IndicatorController();
