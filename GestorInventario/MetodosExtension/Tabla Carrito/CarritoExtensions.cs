using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.MetodosExtension
{
    public static  class CarritoExtensions
    {
        /*DbSet es una clase en Entity Framework que representa un conjunto de entidades que se pueden utilizar 
         * para operaciones de base de datos, como crear, leer, actualizar y eliminar.

         Un DbSet representa la colección de todas las entidades en el contexto, o que se pueden consultar 
        desde la base de datos, de un tipo dado1. Los objetos DbSet se crean a partir de un DbContext utilizando 
        el método DbContext.Set*/
        //La diferencia con los metodos de extension que hemos creado antes es que de esta manera este metodo
        //de extension es para una tabla en concreto mientras que el otro es generico que puedes acceder a todas
        //las tablas si se hubiese pues (this GestorInventarioContext) que extendiese de aqui
        public static Task<Carrito> FindByUserId(this DbSet<Carrito> carritos, int userId)
        {
            return carritos.FirstOrDefaultAsync(c => c.UsuarioId == userId);
        }
    }
}
