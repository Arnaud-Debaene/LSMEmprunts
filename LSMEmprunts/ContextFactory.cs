using LSMEmprunts.Data;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace LSMEmprunts
{
    static class ContextFactory
    {
        static ContextFactory()
        {
            var optionsBuilder = new DbContextOptionsBuilder<Context>();
            optionsBuilder.UseNpgsql(ConfigurationManager.ConnectionStrings["LSMEmprunts"].ConnectionString);
            _ContextOptions = optionsBuilder.Options;

            using var context = OpenContext();
            context.Database.Migrate();
        }

        private readonly static DbContextOptions<Context> _ContextOptions;

        public static Context OpenContext()
        {            
            var retval = new Context(_ContextOptions);
            return retval;
        }
    }
}
