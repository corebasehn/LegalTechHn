using System.Collections.Generic;

namespace LegalTech.Web.Domain.Entities;

/// <summary>
/// Modelo y catálogo oficial completo de las 45 Clases de Niza para selección asistida.
/// </summary>
public class ClaseNizaItem
{
    public int Numero { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Producto"; // Producto (1-34) o Servicio (35-45)

    public string DisplayText => $"Clase {Numero:D2}: {Nombre} ({Categoria})";

    public static List<ClaseNizaItem> ObtenerCatalogoCompleto() => new()
    {
        // ----------------- PRODUCTOS (CLASES 1 A 34) -----------------
        new() { Numero = 1, Nombre = "Productos químicos para industria, ciencia, agricultura y fotografía", Categoria = "Producto" },
        new() { Numero = 2, Nombre = "Pinturas, barnices, lacas y antioxidantes", Categoria = "Producto" },
        new() { Numero = 3, Nombre = "Cosméticos, perfumes, jabones y productos de limpieza", Categoria = "Producto" },
        new() { Numero = 4, Nombre = "Aceites, grasas industriales, lubricantes y combustibles", Categoria = "Producto" },
        new() { Numero = 5, Nombre = "Productos farmacéuticos, veterinarios y suplementos alimenticios", Categoria = "Producto" },
        new() { Numero = 6, Nombre = "Metales comunes, sus aleaciones y materiales de construcción metálicos", Categoria = "Producto" },
        new() { Numero = 7, Nombre = "Máquinas herramientas, motores industriales y aparatos mecánicos", Categoria = "Producto" },
        new() { Numero = 8, Nombre = "Herramientas e instrumentos de mano, cuchillería y navajas", Categoria = "Producto" },
        new() { Numero = 9, Nombre = "Aparatos científicos, computadoras, software, electrónica y seguridad", Categoria = "Producto" },
        new() { Numero = 10, Nombre = "Aparatos e instrumentos médicos, quirúrgicos, dentales y prótesis", Categoria = "Producto" },
        new() { Numero = 11, Nombre = "Aparatos de alumbrado, calefacción, cocción y refrigeración", Categoria = "Producto" },
        new() { Numero = 12, Nombre = "Vehículos y aparatos de locomoción terrestre, aérea o marítima", Categoria = "Producto" },
        new() { Numero = 13, Nombre = "Armas de fuego, municiones, proyectiles y fuegos artificiales", Categoria = "Producto" },
        new() { Numero = 14, Nombre = "Metales preciosos, artículos de joyería, bisutería y relojería", Categoria = "Producto" },
        new() { Numero = 15, Nombre = "Instrumentos musicales y sus accesorios", Categoria = "Producto" },
        new() { Numero = 16, Nombre = "Papel, cartón, artículos de imprenta, encuadernación y papelería", Categoria = "Producto" },
        new() { Numero = 17, Nombre = "Caucho, goma, resinas y materiales plásticos extruidos", Categoria = "Producto" },
        new() { Numero = 18, Nombre = "Cuero, imitaciones, baúles, maletas, billeteras y paraguas", Categoria = "Producto" },
        new() { Numero = 19, Nombre = "Materiales de construcción no metálicos (cemento, asfalto, yeso)", Categoria = "Producto" },
        new() { Numero = 20, Nombre = "Muebles, espejos, marcos y artículos de madera o plástico", Categoria = "Producto" },
        new() { Numero = 21, Nombre = "Utensilios y recipientes domésticos, cocina, vajillas y cristalería", Categoria = "Producto" },
        new() { Numero = 22, Nombre = "Cuerdas, cordeles, redes, lonas, carpas, sacos y velas", Categoria = "Producto" },
        new() { Numero = 23, Nombre = "Hilos e hilados para uso textil", Categoria = "Producto" },
        new() { Numero = 24, Nombre = "Tejidos, mantas, ropa de cama, mantelería y cortinas", Categoria = "Producto" },
        new() { Numero = 25, Nombre = "Prendas de vestir, calzado y artículos de sombrerería", Categoria = "Producto" },
        new() { Numero = 26, Nombre = "Encajes, cintas, botones, cordones, cierres y pasamanería", Categoria = "Producto" },
        new() { Numero = 27, Nombre = "Alfombras, felpudos, esteras y revestimientos para suelos", Categoria = "Producto" },
        new() { Numero = 28, Nombre = "Juegos, juguetes, artículos de deporte y gimnasia", Categoria = "Producto" },
        new() { Numero = 29, Nombre = "Carne, pescado, aves, frutas y verduras en conserva, lácteos", Categoria = "Producto" },
        new() { Numero = 30, Nombre = "Café, té, cacao, azúcar, harinas, pan, confitería y especias", Categoria = "Producto" },
        new() { Numero = 31, Nombre = "Productos agrícolas, granos, frutas frescas y animales vivos", Categoria = "Producto" },
        new() { Numero = 32, Nombre = "Cervezas, aguas minerales, refrescos, bebidas no alcohólicas y zumos", Categoria = "Producto" },
        new() { Numero = 33, Nombre = "Bebidas alcohólicas (excepto cervezas), licores y vinos", Categoria = "Producto" },
        new() { Numero = 34, Nombre = "Tabaco, cigarrillos y artículos para fumadores", Categoria = "Producto" },

        // ----------------- SERVICIOS (CLASES 35 A 45) -----------------
        new() { Numero = 35, Nombre = "Publicidad, gestión de negocios comerciales y administración", Categoria = "Servicio" },
        new() { Numero = 36, Nombre = "Servicios financieros, monetarios, bancarios, seguros e inmobiliarios", Categoria = "Servicio" },
        new() { Numero = 37, Nombre = "Servicios de construcción, instalación, reparación y mantenimiento", Categoria = "Servicio" },
        new() { Numero = 38, Nombre = "Servicios de telecomunicaciones, telefonía e internet", Categoria = "Servicio" },
        new() { Numero = 39, Nombre = "Transporte, logística, embalaje y almacenamiento de mercancías", Categoria = "Servicio" },
        new() { Numero = 40, Nombre = "Tratamiento de materiales, manufactura por encargo y reciclaje", Categoria = "Servicio" },
        new() { Numero = 41, Nombre = "Educación, formación profesional, entretenimiento y cultura", Categoria = "Servicio" },
        new() { Numero = 42, Nombre = "Servicios científicos, tecnológicos, diseño y desarrollo de software", Categoria = "Servicio" },
        new() { Numero = 43, Nombre = "Servicios de restauración (comida/bebida) y hospedaje temporal", Categoria = "Servicio" },
        new() { Numero = 44, Nombre = "Servicios médicos, veterinarios, higiene, estética y agricultura", Categoria = "Servicio" },
        new() { Numero = 45, Nombre = "Servicios jurídicos, legales y de seguridad física o personal", Categoria = "Servicio" }
    };
}
