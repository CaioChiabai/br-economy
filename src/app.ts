import express from 'express';
import cors from 'cors';
import rateLimit from 'express-rate-limit';
import { config } from './config';
import { healthController } from './controllers/health.controller';
import { indicatorController } from './controllers/indicator.controller';
import { errorHandler, notFoundHandler } from './middlewares/error.middleware';

const app = express();

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Rate limiting
const limiter = rateLimit({
  windowMs: config.rateLimit.windowMs,
  max: config.rateLimit.maxRequests,
  message: 'Too many requests from this IP, please try again later.',
  standardHeaders: true,
  legacyHeaders: false,
});

app.use('/api', limiter);

// Routes
app.get('/health', healthController.check.bind(healthController));

// API Routes
app.get('/api/indicators', indicatorController.listIndicators.bind(indicatorController));
app.get('/api/indicators/:type', indicatorController.getIndicator.bind(indicatorController));
app.get('/api/indicators/:type/latest', indicatorController.getLatest.bind(indicatorController));

// Root endpoint
app.get('/', (req, res) => {
  res.json({
    name: 'Brazilian Economic Indicators API',
    version: '1.0.0',
    description: 'High-performance API Hub for Brazilian Economic Indicators',
    endpoints: {
      health: '/health',
      indicators: '/api/indicators',
      getIndicator: '/api/indicators/:type',
      getLatest: '/api/indicators/:type/latest'
    },
    availableIndicators: ['selic', 'ipca', 'cdi', 'igpm', 'dolar']
  });
});

// Error handlers
app.use(notFoundHandler);
app.use(errorHandler);

export default app;
