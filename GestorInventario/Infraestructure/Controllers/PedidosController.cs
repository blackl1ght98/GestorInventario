﻿using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Controllers
{
    public class PedidosController : Controller
    {
        private readonly GestorInventarioContext _context;

        public PedidosController(GestorInventarioContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Producto)
                .Include(p => p.IdUsuarioNavigation)
                .ToListAsync();

            return View(pedidos);
        }
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var model = new PedidosViewModel
            {
                NumeroPedido = GenerarNumeroPedido()
            };
            ViewData["Productos"] = new SelectList(_context.Productos, "Id", "NombreProducto");
            //Convierte el desplegable en una lista
            ViewBag.Productos = _context.Productos.ToList();

            ViewData["Clientes"] = new SelectList(_context.Usuarios, "Id", "NombreCompleto");

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PedidosViewModel model)
        {
            if (ModelState.IsValid)
            {
                var temporal = GenerarNumeroPedido();
                //Crea el pedido
                var pedido = new Pedido()
                {
                    NumeroPedido = model.NumeroPedido = temporal,
                    FechaPedido = model.FechaPedido,
                    EstadoPedido = model.EstadoPedido,
                    IdUsuario = model.IdUsuario,
                };

                _context.Add(pedido);
                await _context.SaveChangesAsync();

                // Crea los detalles del pedido
                //Este bucle for primero inicializa i en 0, despues cuenta el numero total de id de productos que hay y como
                //tu agregas uno ese numero de productos se incrementa, una vez incrementado el numero de productos hace lo siguiente:
                /*
                 * Imagina que tienes una lista de 3 productos, entonces model.IdsProducto.Count sería 3. 
                 * La variable i en el bucle for se inicializa a 0. Aquí está cómo se ejecutaría el bucle:

                Primera iteración: i = 0, ya que 0 < 3 es verdadero, el bucle se ejecuta para el primer producto.
                Segunda iteración: i se incrementa a 1, ya que 1 < 3 es verdadero, el bucle se ejecuta para el segundo producto.
                Tercera iteración: i se incrementa a 2, ya que 2 < 3 es verdadero, el bucle se ejecuta para el tercer producto.
                Cuarta iteración: i se incrementa a 3, pero ya que 3 < 3 es falso, el bucle se detiene.

                 */
                // Aquí se usa el bucle for porque se manejan dos listas. La primera, model.ProductosSeleccionados,
                // almacena qué producto ha sido seleccionado y para ello se necesita la posición[i] de donde está.
                // La segunda lista, model.IdsProducto, almacena los ids ya existentes y al agregar uno, se agrega al final.
                // Por ello, es necesario saber la posición.
                for (var i = 0; i < model.IdsProducto.Count; i++)
                {
                    //Detecta que posicion tiene el producto seleccionado
                    if (model.ProductosSeleccionados[i]) 
                    {
                        //Agrega el producto seleccionado a DetallePedido
                        var detallePedido = new DetallePedido()
                        {
                            PedidoId = pedido.Id,
                            ProductoId = model.IdsProducto[i],
                            Cantidad = model.Cantidad, 
                        };

                        _context.Add(detallePedido);
                    }
                }
                //Crea un despleagable
                ViewData["Productos"] = new SelectList(_context.Productos, "Id", "NombreProducto");
                //Ese desplegable los transforma en una lista
                ViewBag.Productos = _context.Productos.ToList();

                ViewData["Clientes"] = new SelectList(_context.Usuarios, "Id", "NombreCompleto");
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Los datos se han creado con éxito.";

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        private string GenerarNumeroPedido()
        {
            var length = 10; 
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
           .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public async Task<IActionResult> Delete(int id)
        {
            //Consulta a base de datos
            var pedido = await _context.Pedidos
                .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Producto)
                .Include(p => p.IdUsuarioNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            //Si no hay pedidos muestra el error 404
            if (pedido == null)
            {
                return NotFound("Pedido no encontrado");
            }

            //Llegados ha este punto hay pedidos por lo tanto se muestran los pedidos
            return View(pedido);
        }


        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            var pedido = await _context.Pedidos.Include(p => p.DetallePedidos).FirstOrDefaultAsync(m => m.Id == Id);
            if (pedido == null)
            {
                return BadRequest();
            }

            // Elimina los detalles del pedido
            _context.DetallePedidos.RemoveRange(pedido.DetallePedidos);

            // Elimina el pedido
            _context.Pedidos.Remove(pedido);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Los datos se han eliminado con éxito.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> Edit(int id)
        {
            Pedido pedido = await _context.Pedidos
                .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Producto)
                .Include(p => p.IdUsuarioNavigation)
                .FirstOrDefaultAsync(x => x.Id == id);

            PedidosViewModel pedidosViewModel = new PedidosViewModel
            {
                Id = pedido.Id,
                NumeroPedido = pedido.NumeroPedido,
               
                FechaPedido = pedido.FechaPedido,
                EstadoPedido = pedido.EstadoPedido,
                IdUsuario = pedido.IdUsuario
            };

            ViewData["Productos"] = new SelectList(_context.Productos, "Id", "NombreProducto");
            ViewBag.Productos = _context.Productos.ToList();

            ViewData["Clientes"] = new SelectList(_context.Usuarios, "Id", "NombreCompleto");

            return View(pedidosViewModel);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(Pedido pedido)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Actualiza el pedido
                    _context.Entry(pedido).State = EntityState.Modified;

                    // Actualiza los detalles del pedido
                    foreach (var detallePedido in pedido.DetallePedidos)
                    {
                        _context.Entry(detallePedido).State = EntityState.Modified;
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Los datos se han modificado con éxito.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PedidoExist(pedido.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        _context.Entry(pedido).Reload();

                        // Intenta guardar de nuevo
                        _context.Entry(pedido).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                }
                return RedirectToAction("Index");
            }
            return View(pedido);
        }

        private bool PedidoExist(int Id)
        {

            return _context.Pedidos.Any(e => e.Id == Id);
        }
    }
}