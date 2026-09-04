
using minimal_api.Dominio.Enuns;

namespace minimal_api.Dominio.ModelViews
{
    public class AdministradorModelView
    {
         public int Id { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Senha { get; set; } = default!;
        public Perfil Perfil { get; set; } = default!;

    }
}