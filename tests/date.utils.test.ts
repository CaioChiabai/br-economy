import { isValidDate } from '../src/utils/date.utils';

describe('Date Utils', () => {
  describe('isValidDate', () => {
    it('should validate correct date format DD/MM/YYYY', () => {
      expect(isValidDate('01/01/2024')).toBe(true);
      expect(isValidDate('31/12/2023')).toBe(true);
      expect(isValidDate('15/06/2024')).toBe(true);
    });

    it('should reject invalid date formats', () => {
      expect(isValidDate('2024-01-01')).toBe(false);
      expect(isValidDate('01-01-2024')).toBe(false);
      expect(isValidDate('1/1/2024')).toBe(false);
      expect(isValidDate('01/1/2024')).toBe(false);
    });

    it('should reject invalid dates', () => {
      expect(isValidDate('32/01/2024')).toBe(false);
      expect(isValidDate('31/02/2024')).toBe(false);
      expect(isValidDate('00/01/2024')).toBe(false);
      expect(isValidDate('01/13/2024')).toBe(false);
    });

    it('should handle leap years correctly', () => {
      expect(isValidDate('29/02/2024')).toBe(true); // 2024 is a leap year
      expect(isValidDate('29/02/2023')).toBe(false); // 2023 is not a leap year
    });
  });
});
