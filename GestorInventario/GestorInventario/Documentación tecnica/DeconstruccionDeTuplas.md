**Documentación sobre la Deconstrucción de Tuplas en C#**
**¿Que es la deconstrucción de tuplas**
La deconstrucción de tuplas es una técnica en la **programación orientada a objetos** que permite descomponer una tupla, que es un tipo de valor que puede contener múltiples 
valores de diferentes tipos, en variables separadas. Esto facilita el manejo de múltiples valores de retorno de manera clara y concisa.


**Método de Ejemplo:**

En el siguiente método, se aplica la **deconstrucción de tuplas** para retornar el estado de una operación de eliminación del producto junto con los mensajes de error.
**Como realizar la deconstrucción de tuplas:**
Para realizar la **deconstrucción de tuplas** tenemos primero decir que **tipo de valor** van a manejar esas tuplas en este metodo de ejemplo recibe un tipo **bool** y un
tipo **string**, para indicarlo se pone asi: `Task<(bool, string)>`.
public async Task<(bool, string)> EliminarProducto(int Id)
{
var producto = await _context.Productos.Include(p => p.DetallePedidos)
        .ThenInclude(dp => dp.Pedido)
        .Include(p => p.IdProveedorNavigation)
        .Include(x => x.DetalleHistorialProductos)
        .FirstOrDefaultAsync(m => m.Id == Id);

if (producto == null)
{
A la hora de **devolver valores** es obligatorio respetar el tipo de dato que hemos puesto a lo primero del metodo en este caso `Task<(bool, string)>` el orden es importante
primero hay que pasarle un valor **bool** y despues un **string** si no se respeta el orden se producira un error. Indicar tambien que cuando se esta trabajando con la deconstrucción
de tuplas cuando se devuelve va entre `()` y cada valor va separado con `,`

 **return (false, "No hay productos para eliminar");**
}

if (producto.DetallePedidos.Any() || producto.DetalleHistorialProductos.Any())
{
   return (false, "El producto no se puede eliminar porque tiene pedidos o historial asociados.");
}

_context.DeleteEntity(producto);
return (true, null);
}

**Uso en el Controlador**

public async Task<IActionResult> Delete(int id)
{
    try
    {
        var policy = _PolicyHandler.GetRetryPolicyAsync();
        var producto = await ExecutePolicyAsync(() => _productoRepository.EliminarProductoObtencion(id));

if (producto == null)
{
TempData["ErrorMessage"] = "Producto no encontrado";
return NotFound("Producto no encontrado");
}

return View(producto);
}
catch (Exception ex)
{
    TempData["ErrorMessage"] = "Tiempo de respuesta del servidor agotado";
    _logger.LogError(ex, "Error al eliminar el producto");
    return BadRequest("Error al mostrar la vista de eliminación. Inténtelo de nuevo más tarde. Si el problema persiste, contacte con el administrador.");
  }
}

[HttpPost, ActionName("DeleteConfirmed")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int Id)
{
    var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
    int usuarioId;

if (int.TryParse(existeUsuario, out usuarioId))
{
En el controlador, el método `EliminarProducto` es invocado y su resultado es desestructurado en variables individuales usando la deconstrucción de tuplas. Estas variables 
adquieren el tipo de dato que hemos puesto en la **deconstruccion de tuplas** en este caso `success` admite solo `true o false` que estos valores son `booleanos o bool` y `errorMessage`
solo admitira valores de tipo texto `string` esto permite una mayor flexibilidad en el manejo de datos y consistencia.

**var (success, errorMessage) = await ExecutePolicyAsync(() => _productoRepository.EliminarProducto(Id));**
if (success)
    {
      TempData["SuccessMessage"] = "Los datos se han eliminado con éxito.";
      return RedirectToAction(nameof(Index));
    }
else
    {
    TempData["ErrorMessage"] = errorMessage;
    return RedirectToAction(nameof(Delete), new { id = Id });
    }
 }

 return BadRequest("Error al eliminar el producto. Inténtelo de nuevo más tarde. Si el problema persiste, contacte con el administrador.");
}
**Ventajas de la deconstrucción de tupal**

- **Facilidad a la hora de manejar datos**
- **Codigo mas legible**
- **Facilidad en el matenimiento del codigo**


