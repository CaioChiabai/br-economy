namespace BrEconomy.API.Entities
{
    public class EconomicIndicator
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Ex: "SELIC", "IPCA", "CDI"
        public string Name { get; set; } = string.Empty;

        // O valor do indicador (ex: 11.25)
        public decimal Value { get; set; }

        // A data a que se refere o dado (ex: data da reunião do Copom)
        public DateTime ReferenceDate { get; set; }

        // Quando nós atualizamos esse dado no nosso banco
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
