using RedisUI.Contents;
using RedisUI.Models;
using System;
using System.Text;

namespace RedisUI.Pages
{
    public static class Layout
    {
        public static string Build(LayoutModel model, RedisUISettings settings)
        {
            var dbList = new StringBuilder();
            foreach (var item in model.DbList)
            {
                dbList.Append($"<li><a class=\"dropdown-item\" id=\"nav{item}\" href=\"javascript:setdb({item});\">{item}</a></li>");
            }

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Redis Integrated UI</title>
    <link href=""{settings.CssLink}"" rel=""stylesheet"" crossorigin=""anonymous"">
    <script src=""{settings.JsLink}"" crossorigin=""anonymous""></script>

    <style>
        .dropdown-menu {{
            z-index: 1021;
        }}

    </style>

<script>

    function setdb(db){{
        var currentPath = window.location.href.replace(window.location.search, '');
        window.location = currentPath.replace('#', '') + '?size=10&cursor=0&db=' + db;
    }}

    function setSize(size) {{
        const url = new URL(window.location);
        const params = url.searchParams;

        // Si no había db, la establecemos a '0'
        if (!params.has('db')) {{
            params.set('db', '0');
        }}

        // Siempre actualizamos size y reiniciamos cursor
        params.set('size', size);
        params.set('cursor', '0');

        // Reconstruimos la búsqueda y navegamos
        url.search = params.toString();
        window.location = url.pathname + url.search;
    }}

</script>

</head>
<body>
    
    <nav class=""navbar navbar-expand-lg bg-dark navbar-dark"">
        <div class=""container-fluid"">
            <a class=""navbar-brand"" href=""..{settings.Path}"">RedisUI</a>
            <div class=""collapse navbar-collapse"" id=""navbarSupportedContent"">
              <ul class=""navbar-nav me-auto mb-2 mb-lg-0"">
                <a class=""navbar-brand"" title=""Keys"">
                    {Icons.KeyLg}      
                    {model.DbSize}
                </a>
                <li class=""nav-item dropdown"">
                  <a id=""dblink"" class=""nav-link dropdown-toggle"" href=""#"" role=""button"" data-bs-toggle=""dropdown"" aria-expanded=""false"">
                    DB ({model.CurrentDb})
                  </a>
                  <ul class=""dropdown-menu"">
                    {dbList}
                  </ul> 
                </li>
              </ul>
            </div>            
            <a class=""navbar-brand"" title=""Statistics"" href=""..{settings.Path}/statistics"">
                {Icons.Statistic}
            </a>
        </div>
    </nav>

    <div class=""container"">
        <br/>
            {model.Section}
    </div>

    <div class=""container"">
        <div class=""row"">
        <footer class=""d-flex flex-wrap justify-content-between align-items-center py-3 my-4 border-top"">
            <div class=""col-md-4 d-flex align-items-center"">
              <span class=""mb-3 mb-md-0 text-body-secondary"">© {DateTime.Now.Year} Redis Integrated UI</span>
            </div>
        </footer>
    </div></div>
</body>
</html>
";

        }
    }
}
