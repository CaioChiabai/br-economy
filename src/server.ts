import app from './app';
import { config } from './config';

const startServer = () => {
  try {
    app.listen(config.port, () => {
      console.log(`
╔════════════════════════════════════════════════════════════╗
║  Brazilian Economic Indicators API                         ║
║  High-Performance Hub for Economic Data                    ║
╚════════════════════════════════════════════════════════════╝

✓ Server running on port ${config.port}
✓ Environment: ${config.nodeEnv}
✓ Cache enabled with TTL configuration
✓ Rate limiting: ${config.rateLimit.maxRequests} requests per ${config.rateLimit.windowMs}ms

Available endpoints:
  - GET /health
  - GET /api/indicators
  - GET /api/indicators/:type
  - GET /api/indicators/:type/latest

Supported indicators: selic, ipca, cdi, igpm, dolar

Ready to serve requests! 🚀
      `);
    });
  } catch (error) {
    console.error('Failed to start server:', error);
    process.exit(1);
  }
};

// Handle uncaught exceptions
process.on('uncaughtException', (error) => {
  console.error('Uncaught Exception:', error);
  process.exit(1);
});

process.on('unhandledRejection', (reason, promise) => {
  console.error('Unhandled Rejection at:', promise, 'reason:', reason);
  process.exit(1);
});

startServer();
