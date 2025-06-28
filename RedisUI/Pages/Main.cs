using RedisUI.Contents;
using RedisUI.Helpers;
using RedisUI.Models;
using System.Collections.Generic;
using System.Text;

namespace RedisUI.Pages
{
    public static class Main
    {
        public static string Build(List<KeyModel> keys, long next)
        {
            var tbody = new StringBuilder();
            foreach (var key in keys)
            {
                var columns = $"<td><span class=\"badge text-bg-{key.Badge}\">{key.KeyType}</span></td><td>{key.Name}</td><td>{key.Value.Length().ToKilobytes()}</td>";

                tbody.Append($"<tr style=\"cursor: pointer;\" data-value='{key.Value}'>{columns}<td><a onclick=\"confirmDelete('{key.Name}')\" class=\"btn btn-sm btn-outline-danger\"><span>{Icons.Delete}</span></a></td></tr>");
            }

            var html = $@"
    {InsertModal.Build()}
  <div class=""container-fluid"">
        <div class=""row align-items-center mb-3"">
          <div class=""col-sm-12 col-md-6"">
            <div id=""search"" class=""input-group"">
              <!-- Search input injected here -->
            </div>
          </div>
          <div class=""col-sm-12 col-md-6"">
            <div class=""d-flex justify-content-end align-items-center gap-2 flex-nowrap"">
              <button type=""button"" class=""btn btn-success"" data-bs-toggle=""modal"" data-bs-target=""#insertModal"" title=""Add or Edit Key"">
                {Icons.KeyLg} Add/Edit
              </button>
              <div class=""btn-group btn-group-sm"" role=""group"" aria-label=""Page size"">
                <button type=""button"" class=""btn btn-outline-secondary"" id=""size10"" onclick=""setSize(10)"">10</button>
                <button type=""button"" class=""btn btn-outline-secondary"" id=""size20"" onclick=""setSize(20)"">20</button>
                <button type=""button"" class=""btn btn-outline-secondary"" id=""size50"" onclick=""setSize(50)"">50</button>
                <button type=""button"" class=""btn btn-outline-secondary"" id=""size100"" onclick=""setSize(100)"">100</button>
                <button type=""button"" class=""btn btn-outline-secondary"" id=""size500"" onclick=""setSize(500)"">500</button>
                <button type=""button"" class=""btn btn-outline-secondary"" id=""size1000"" onclick=""setSize(1000)"">1000</button>
              </div>
            </div>
          </div>
        </div>
    </div>

    <div class=""container-fluid"">
        <div class=""row g-3"">
            <div class=""col-lg-6"">
                <div class=""table-responsive card shadow-sm"">
                    <table class=""table table-striped table-hover mb-0"" id=""redisTable"">
                        <thead class=""table-primary sticky-top"">
                            <tr>
                                <th>Type</th>
                                <th>Key</th>
                                <th>Size (KB)</th>
                                <th class=""text-center"">Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            {tbody}
                        </tbody>
                    </table>
                </div>
                <div class=""d-flex justify-content-center my-2"">
                    <button id=""btnNext"" class=""btn btn-primary"" onclick=""nextPage()"">
                        {(next == 0 ? "Back to Top" : "Next")}
                    </button>
                </div>
            </div>

            <div class=""col-lg-6"">
                <div class=""card shadow-sm h-100"">
                    <div class=""card-header bg-info text-white"">Value</div>
                    <div class=""card-body overflow-auto"">
                        <pre class=""mb-0""><code id=""valueContent"">Click on a key to get value...</code></pre>
                    </div>
                </div>
            </div>
        </div>
    </div>

<script>
document.addEventListener('DOMContentLoaded', function () {{
    const params = new URLSearchParams(window.location.search);

    // 1. Leer parámetros y decodificar el key
    const cursor = params.get('cursor') || '0';
    const db     = params.get('db')     || '0';
    const key    = params.has('key')    ? decodeURIComponent(params.get('key')) : '';
    const size   = params.get('size')   || '10';

    // 2. Inyectar búsqueda con valor decodificado
    const searchContainer = document.getElementById('search');
    searchContainer.innerHTML = '';
    const sInput = document.createElement('input');
    sInput.type = 'text';
    sInput.className = 'form-control';
    sInput.placeholder = 'key or pattern...';
    sInput.value = key;
    const sBtn = document.createElement('button');
    sBtn.innerText = 'Search';
    sBtn.className = 'btn btn-outline-success btn-sm ms-2';
    sBtn.onclick = () => showPage(0, db, sInput.value);
    searchContainer.append(sInput, sBtn);

    // 3. Marcar menú y tamaño activos
    document.getElementById('nav' + new URLSearchParams(window.location.search).get('db'))?.classList.add('active');
    document.getElementById('size' + new URLSearchParams(window.location.search).get('size'))?.classList.add('active');

    // 4. Asignar click a las filas
    document.querySelectorAll('#redisTable tbody tr').forEach(row => {{
      row.onclick = () => {{
        const val = row.getAttribute('data-value');
        document.getElementById('valueContent').textContent = JSON.stringify(val, null, 4);
      }};
    }});

    // 5. Configurar botón Next/Back
    document.getElementById('btnNext').onclick = () => nextPage();
    document.getElementById('btnNext').hidden = ('0' === '{next}');
  }});

function showPage(cursor, db, key) {{
  const params = new URLSearchParams();
  params.set('cursor', cursor);
  params.set('db', db);

  const activeSizeBtn = document.querySelector('.btn-group button.active');
  const size = activeSizeBtn ? activeSizeBtn.textContent : '10';
  params.set('size', size);

  if (key) params.set('key', key);
  window.location = window.location.pathname + '?' + params.toString();
}}


function nextPage() {{
    showPage({next}, 
            new URLSearchParams(window.location.search).get('db') || '0',
            new URLSearchParams(window.location.search).get('key') || '');
}}

function confirmDelete(del) {{
    if (!confirm(`Are you sure to delete key '${{del}}'?`)) return;
    fetch(window.location.href, {{
        method: 'POST',
        body: JSON.stringify({{ DelKey: del }}),
        headers: {{ 'Content-Type': 'application/json' }}
    }}).then(() => window.location.reload());
}}

function saveKey() {{
    fetch(window.location.href, {{
        method: 'POST',
        body: JSON.stringify({{
            InsertKey: document.getElementById('insertKey').value,
            InsertValue: document.getElementById('insertValue').value
        }}),
        headers: {{ 'Content-Type': 'application/json' }}
    }}).then(() => window.location.reload());
}}

function checkRequired() {{
    const k = document.getElementById('insertKey').value;
    const v = document.getElementById('insertValue').value;
    document.getElementById('btnSave').disabled = !(k && v);
}}
</script>
";
            return html;
        }
    }
}
