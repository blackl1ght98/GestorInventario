**Documentaci�n sobre la Deconstrucci�n de Tuplas en C#**
**�Que es la deconstrucci�n de tuplas**
La deconstrucci�n de tuplas es una t�cnica en la **programaci�n orientada a objetos** que permite descomponer una tupla, que es un tipo de valor que puede contener m�ltiples 
valores de diferentes tipos, en variables separadas. Esto facilita el manejo de m�ltiples valores de retorno de manera clara y concisa.


**M�todo de Ejemplo:**

En el siguiente m�todo, se aplica la **deconstrucci�n de tuplas** para retornar el estado de una operaci�n de eliminaci�n del producto junto con los mensajes de error.
**Como realizar la deconstrucci�n de tuplas:**
Para realizar la **deconstrucci�n de tuplas** tenemos primero decir que **tipo de valor** van a manejar esas tuplas en este metodo de ejemplo recibe un tipo **bool** y un
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
primero hay que pasarle un valor **bool** y despues un **string** si no se respeta el orden se producira un error. Indicar tambien que cuando se esta trabajando con la deconstrucci�n
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
    return BadRequest("Error al mostrar la vista de eliminaci�n. Int�ntelo de nuevo m�s tarde. Si el problema persiste, contacte con el administrador.");
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
En el controlador, el m�todo `EliminarProducto` es invocado y su resultado es desestructurado en variables individuales usando la deconstrucci�n de tuplas. Estas variables 
adquieren el tipo de dato que hemos puesto en la **deconstruccion de tuplas** en este caso `success` admite solo `true o false` que estos valores son `booleanos o bool` y `errorMessage`
solo admitira valores de tipo texto `string` esto permite una mayor flexibilidad en el manejo de datos y consistencia.

**var (success, errorMessage) = await ExecutePolicyAsync(() => _productoRepository.EliminarProducto(Id));**
if (success)
    {
      TempData["SuccessMessage"] = "Los datos se han eliminado con �xito.";
      return RedirectToAction(nameof(Index));
    }
else
    {
    TempData["ErrorMessage"] = errorMessage;
    return RedirectToAction(nameof(Delete), new { id = Id });
    }
 }

 return BadRequest("Error al eliminar el producto. Int�ntelo de nuevo m�s tarde. Si el problema persiste, contacte con el administrador.");
}
**Ventajas de la deconstrucci�n de tupal**

- **Facilidad a la hora de manejar datos**
- **Codigo mas legible**
- **Facilidad en el matenimiento del codigo**


