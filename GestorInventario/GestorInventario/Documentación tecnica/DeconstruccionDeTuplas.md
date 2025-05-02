### Documentaci�n sobre la Deconstrucci�n de Tuplas en C#
## �Que es la deconstrucci�n de tuplas
La deconstrucci�n de tuplas es una t�cnica en la **programaci�n orientada a objetos** que permite descomponer una tupla, que es un tipo de valor que puede contener m�ltiples 
valores de diferentes tipos, en variables separadas. Esto facilita el manejo de m�ltiples valores de retorno de manera clara y concisa.


### M�todo de Ejemplo:

En el siguiente m�todo, se aplica la **deconstrucci�n de tuplas** para retornar el estado de una operaci�n de eliminaci�n del producto junto con los mensajes de error.

# Como realizar la deconstrucci�n de tuplas:
Para realizar la **deconstrucci�n de tuplas** tenemos primero decir que **tipo de valor** van a manejar esas tuplas en este metodo de ejemplo recibe un tipo **bool** y un
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
### �Como se devuelve los valores?
A la hora de **devolver valores** es obligatorio respetar el tipo de dato que hemos puesto a lo primero del metodo en este caso `Task<(bool, string)>` el orden es importante
primero hay que pasarle un valor **bool** y despues un **string** si no se respeta el orden se producira un error. Indicar tambien que cuando se esta trabajando con la deconstrucci�n
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


var (success, errorMessage) = await ExecutePolicyAsync(() => _productoRepository.EliminarProducto(Id));
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
````
### Como llamar al metodo que tiene aplicado la deconstrucci�n de tuplas
En el controlador se llama al metodo `EliminarProducto` y se le asigna a una variable de tipo `var` que es la que va a recibir los valores de la tupla,
```csharp
var (success, errorMessage) = await ExecutePolicyAsync(() => _productoRepository.EliminarProducto(Id));
```
### �Como se accede a los valores?
Para acceder a los valores de la tupla se hace de la siguiente manera:
```csharp
var (success, errorMessage) = await ExecutePolicyAsync(() => _productoRepository.EliminarProducto(Id));
```
- `success` : Este es el primer valor de la tupla, que es de tipo **bool** y indica si la operaci�n fue exitosa o no.
- `errorMessage` : Este es el segundo valor de la tupla, que es de tipo **string** y contiene un mensaje de error en caso de que la operaci�n no haya sido exitosa.
Como el primer dato es un booleano ponemos una condicion para comprobar si es verdadero o falso:
```csharp
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
```
### Ventajas de la deconstrucci�n de tuplas
La deconstrucci�n de tuplas ofrece varias ventajas en la programaci�n, especialmente en el contexto de la programaci�n orientada a objetos. 
Algunas de estas ventajas incluyen:
- **Facilidad para manejar m�ltiples valores de retorno**: Permite devolver m�ltiples valores de diferentes tipos sin necesidad de crear una clase o 
estructura adicional.
- **C�digo m�s legible**: La deconstrucci�n de tuplas hace que el c�digo sea m�s limpio y f�cil de entender, ya que los valores se pueden asignar 
directamente a variables con nombres significativos.
- **Facilidad en el mantenimiento del c�digo**: Al utilizar tuplas, se reduce la necesidad de crear clases o estructuras adicionales, lo que simplifica
 el mantenimiento del c�digo. Adem�s, si se necesita cambiar el n�mero o tipo de valores devueltos, solo se debe modificar la tupla en lugar de
 actualizar m�ltiples clases o estructuras.
- **Reducci�n de la complejidad**: La deconstrucci�n de tuplas permite simplificar el c�digo al evitar la creaci�n de clases o estructuras adicionales para
almacenar valores temporales. Esto reduce la complejidad del c�digo y facilita su comprensi�n.
- **Facilidad para trabajar con APIs y bibliotecas**: Muchas bibliotecas y APIs modernas utilizan tuplas para devolver m�ltiples valores, lo que facilita la integraci�n
con estas herramientas sin necesidad de crear clases o estructuras adicionales.
Estas son solo algunas ventajas pero tiene muchas mas




