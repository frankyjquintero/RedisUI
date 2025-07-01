using RedisUI.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedisUI.Pages
{
    public static class Layout
    {
        public static string Build(LayoutModel model, RedisUISettings settings)
        {
            var dbList = BuildDbList(model?.DbList);
            var head = BuildHead(settings);
            var nav = BuildNav(model, settings, dbList);
            var section = model?.Section ?? "";
            var footer = BuildFooter();

            return $@"<!DOCTYPE html>
                <html lang=""en"">
                {head}
                <body>
                  <div id=""render-ui"">
                    {nav}
                    <div class=""container-fluid"">
                        <br/>
                        {section}
                    </div>
                    {footer}
                  </div>
                </body>
                </html>
            ";
        }

        private static string BuildDbList(List<string> dbList)
        {
            if (dbList == null) return string.Empty;
            var sb = new StringBuilder();
            foreach (var item in dbList)
            {
                sb.Append($"<li><a class=\"dropdown-item\" id=\"nav{item}\" href=\"javascript:setdb({item});\">{item}</a></li>");
            }
            return sb.ToString();
        }

        private static string BuildHead(RedisUISettings settings)
        {
            return $@"
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Redis Integrated UI Dashboard</title>
                <link href=""{settings?.CssLink ?? ""}"" rel=""stylesheet"" crossorigin=""anonymous"">
                <script src=""{settings?.JsLink ?? ""}"" crossorigin=""anonymous""></script>
                <link href=""{settings?.BootstrapIcons ?? ""}"" rel=""stylesheet"" crossorigin=""anonymous"">
                <link href=""{settings?.HighlightTheme ?? ""}"" rel=""stylesheet"">
                <script src=""{settings?.HighlightJs ?? ""}""></script>
                <script src=""{settings?.HighlightJson ?? ""}""></script>
                <link href=""{settings?.JsonEditorCss ?? ""}"" rel=""stylesheet"" type=""text/css"">
                <script src=""{settings?.JsonEditorJs ?? ""}""></script>                

                <style>
                    .dropdown-menu {{ z-index: 1021; }}
                </style>
            </head>";
        }

        private static string BuildNav(LayoutModel model, RedisUISettings settings, string dbList)
        {
            return $@"
            <nav class=""navbar navbar-expand-lg bg-dark navbar-dark"">
                <div class=""container-fluid"">
                    <a class=""navbar-brand"" href=""..{settings?.Path ?? ""}"">RedisUI</a>
                    <div class=""collapse navbar-collapse"" id=""navbarSupportedContent"">
                        <ul class=""navbar-nav me-auto mb-2 mb-lg-0"">
                            <a class=""navbar-brand"" title=""Keys"">
                                <i class=""bi bi-key-fill""></i>
                                {model?.DbSize ?? ""}
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
                    <a class=""navbar-brand"" title=""Statistics"" href=""..{settings?.Path ?? ""}/statistics"">
                        <i class=""bi bi-graph-up""></i>
                    </a>
                    <button class=""btn btn-sm btn-outline-light ms-2"" onclick=""logout()"" title=""Logout"">
                        <i class=""bi bi-box-arrow-right""></i>
                    </button>
                </div>
            </nav>
            <script>
                function logout() {{
                    // Auth Basic
                    if (confirm('Are you sure you want to log out?')) {{
                         document.getElementById('render-ui').innerHTML = `
                            <div class=""container text-center mt-5"">
                            <h2 class=""text-danger"">Session expired or logged out</h2>
                            <p>Please <a href=""${{window.location.pathname}}"">reload</a> to log in again.</p>
                            </div>
                        `;
                        fetch(""..{settings?.Path ?? ""}/logout"")
                        .then(data => {{
                            if (data.status === 401) {{
                                window.location.href = ""..{settings?.Path ?? ""}"";
                            }}
                        }});
                    }}
                    

                }}
            </script>";
        }

        private static string BuildFooter()
        {
            return $@"
            <div class=""container"">
                <div class=""row"">
                    <footer class=""d-flex flex-wrap justify-content-between align-items-center py-3 my-4 border-top"">
                        <div class=""col-md-4 d-flex align-items-center"">
                            <span class=""mb-3 mb-md-0 text-body-secondary"">© {DateTime.Now.Year} Redis Integrated UI</span>
                        </div>
                    </footer>
                </div>
            </div>";
        }
    }
}
