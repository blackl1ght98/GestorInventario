using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;

namespace GestorInventario.MetodosExtension
{
    public static class SelectListExtensions
    {
        public static IEnumerable<SelectListItem> ToSelectList<T>(this IEnumerable<T> items, Expression<Func<T, object>> valueSelector,
            Expression<Func<T, object>> textSelector, object? selectedValue = null, string? placeholder = "Seleccione una opción...")
        { 
        var compiledValue = valueSelector.Compile();
        var compiledText = textSelector.Compile();
            var selectList = items.Select(item => new SelectListItem
            {
                Value = compiledValue(item)?.ToString(),
                Text = compiledText(item)?.ToString(),
                Selected = selectedValue != null && compiledValue(item)?.ToString() == selectedValue,
            }).ToList();
        if(!string.IsNullOrEmpty(placeholder))
            {
                selectList.Insert(0, new SelectListItem
                {
                    Value = "",
                    Text = placeholder,
                    Disabled = true,
                    Selected = selectedValue == null,
                });
            }
        return selectList;
        
        }

    }
}
