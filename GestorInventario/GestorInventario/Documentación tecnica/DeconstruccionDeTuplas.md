### Documentación sobre la Deconstrucción de Tuplas en C#
## ¿Que es la deconstrucción de tuplas
La deconstrucción de tuplas es una técnica en la **programación orientada a objetos** que permite descomponer una tupla, que es un tipo de valor que puede contener múltiples 
valores de diferentes tipos, en variables separadas. Esto facilita el manejo de múltiples valores de retorno de manera clara y concisa.


### Método de Ejemplo:

En el siguiente método, se aplica la **deconstrucción de tuplas** para retornar el estado de una operación de eliminación del producto junto con los mensajes de error.

# Como realizar la deconstrucción de tuplas:
Para realizar la **deconstrucción de tuplas** tenemos primero decir que **tipo de valor** van a manejar esas tuplas en este metodo de ejemplo recibe un tipo **bool** y un
tipo **string**, para indicarlo se pone asi: `Task<(bool, string)>`.
```csharp
public async Task<(bool, string)> EliminarProducto(int Id)
{
var producto = await _context.Productos.Include(p => p.DetallePedidos)
        .ThenInclude(dp => dp.Pedido)
        .Include(p => p.IdProveedorNavigation)
        .Include(x => x.DetalleHistorialProductos)
        .FirstOrDefaultAsync(m => m.Id == Id);

if (producto == null)
{


 **return (false, "No hay productos para eliminar");**
}

if (producto.DetallePedidos.Any() || producto.DetalleHistorialProductos.Any())
{
   return (false, "El producto no se puede eliminar porque tiene pedidos o historial asociados.");
}

_context.DeleteEntity(producto);
return (true, null);
}
```
### ¿Como se devuelve los valores?
A la hora de **devolver valores** es obligatorio respetar el tipo de dato que hemos puesto a lo primero del metodo en este caso `Task<(bool, string)>` el orden es importante
primero hay que pasarle un valor **bool** y despues un **string** si no se respeta el orden se producira un error. Indicar tambien que cuando se esta trabajando con la deconstrucción
de tuplas cuando se devuelve va entre `()` y cada valor va separado con `,`

# Uso en el Controlador**
```csharp
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


var (success, errorMessage) = await ExecutePolicyAsync(() => _productoRepository.EliminarProducto(Id));
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
````
### Como llamar al metodo que tiene aplicado la deconstrucción de tuplas
En el controlador se llama al metodo `EliminarProducto` y se le asigna a una variable de tipo `var` que es la que va a recibir los valores de la tupla,
```csharp
var (success, errorMessage) = await ExecutePolicyAsync(() => _productoRepository.EliminarProducto(Id));
```
### ¿Como se accede a los valores?
Para acceder a los valores de la tupla se hace de la siguiente manera:
```csharp
var (success, errorMessage) = await ExecutePolicyAsync(() => _productoRepository.EliminarProducto(Id));
```
- `success` : Este es el primer valor de la tupla, que es de tipo **bool** y indica si la operación fue exitosa o no.
- `errorMessage` : Este es el segundo valor de la tupla, que es de tipo **string** y contiene un mensaje de error en caso de que la operación no haya sido exitosa.
Como el primer dato es un booleano ponemos una condicion para comprobar si es verdadero o falso:
```csharp
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
```
### Ventajas de la deconstrucción de tuplas
La deconstrucción de tuplas ofrece varias ventajas en la programación, especialmente en el contexto de la programación orientada a objetos. 
Algunas de estas ventajas incluyen:
- **Facilidad para manejar múltiples valores de retorno**: Permite devolver múltiples valores de diferentes tipos sin necesidad de crear una clase o 
estructura adicional.
- **Código más legible**: La deconstrucción de tuplas hace que el código sea más limpio y fácil de entender, ya que los valores se pueden asignar 
directamente a variables con nombres significativos.
- **Facilidad en el mantenimiento del código**: Al utilizar tuplas, se reduce la necesidad de crear clases o estructuras adicionales, lo que simplifica
 el mantenimiento del código. Además, si se necesita cambiar el número o tipo de valores devueltos, solo se debe modificar la tupla en lugar de
 actualizar múltiples clases o estructuras.
- **Reducción de la complejidad**: La deconstrucción de tuplas permite simplificar el código al evitar la creación de clases o estructuras adicionales para
almacenar valores temporales. Esto reduce la complejidad del código y facilita su comprensión.
- **Facilidad para trabajar con APIs y bibliotecas**: Muchas bibliotecas y APIs modernas utilizan tuplas para devolver múltiples valores, lo que facilita la integración
con estas herramientas sin necesidad de crear clases o estructuras adicionales.
Estas son solo algunas ventajas pero tiene muchas mas




