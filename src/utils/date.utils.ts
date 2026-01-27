export const isValidDate = (dateStr: string): boolean => {
  // Validate DD/MM/YYYY format
  const regex = /^\d{2}\/\d{2}\/\d{4}$/;
  if (!regex.test(dateStr)) {
    return false;
  }
  
  const [day, month, year] = dateStr.split('/').map(Number);
  const date = new Date(year, month - 1, day);
  
  return (
    date.getFullYear() === year &&
    date.getMonth() === month - 1 &&
    date.getDate() === day
  );
};

export const formatDateForAPI = (dateStr: string): string => {
  // Returns the date in DD/MM/YYYY format (already in correct format)
  return dateStr;
};
