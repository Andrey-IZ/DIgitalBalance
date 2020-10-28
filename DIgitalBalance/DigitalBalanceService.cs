using System.ServiceProcess;

namespace DigitalBalance
{
    public partial class DigitalBalanceService : ServiceBase
    {
        private readonly DigitalBalanceManager service = new DigitalBalanceManager();
        public DigitalBalanceService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            service.Start();
        }

        protected override void OnStop()
        {
            service.Stop();
        }
    }
}
