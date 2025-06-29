using RedisUI.Contents;
using RedisUI.Helpers;
using RedisUI.Models;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace RedisUI.Pages
{
    public static class Main
    {
        public static string Build(List<KeyModel> keys, long next)
        {
            var tbody = BuildTableBody(keys);
            var listView = BuildTable(tbody);
            var treeView = BuildTreeView(keys);

            var html = $@"
                {InsertModal.Build()}
                {BuildHeader(next)}
                <div class=""row justify-content-start"">
                    <div class='col-8' style='max-height: 750px; overflow-y: auto;'>
                      <div id='listView' class='d-none'>
                         {listView}
                      </div>
                      <div id='treeView'>
                         {treeView}
                      </div>
                    </div>
                    <div class='col-4'  style='max-height: 750px; overflow-y: auto;'>
                     {BuildValuePanel()}
                    </div>
                </div>
                <script>
                    {BuildScript(next)}
                </script>
            ";

            return html;
        }


        private static string BuildTreeView(List<KeyModel> keys)
        {
            var tree = TreeHelper.Build(keys);
            var html = new StringBuilder();
            html.Append("<div class='card p-3'><ul class='list-group'>");
            TreeHelper.RenderTree(tree, html);
            html.Append("</ul></div>");
            return html.ToString();
        }


        private static string BuildTableBody(List<KeyModel> keys)
        {
            var tbody = new StringBuilder();
            foreach (var key in keys)
            {
                var json = JsonSerializer.Serialize(key.Detail.Value);
                var columns = $"<td><span class=\"badge {key.Detail.Badge}\">{key.KeyType.ToString().ToUpper()}</span></td><td>{key.Name}</td><td>{key.Detail.Length}</td><td>{(key.Detail.TTL.HasValue ? $"{key.Detail.TTL} s" : "∞")}</td>";
                tbody.Append($"<tr style=\"cursor: pointer;\" data-value='{json}' data-key='{key.Name}'>{columns}<td class=\"text-center\"><a onclick=\"confirmDelete('{key.Name}')\" class=\"btn btn-sm btn-outline-danger\"><span>{Icons.Delete}</span></a></td></tr>");
            }
            return tbody.ToString();
        }

        private static string BuildHeader(long next) => $@"
          <div class=""container-fluid"">
            <div class=""row align-items-center mb-3"">
              <div class=""col-sm-12 col-md-6"">
                <div id=""search"" class=""input-group""></div>
              </div>
              <div class=""col-sm-12 col-md-6"">
                <div class=""d-flex justify-content-end align-items-center gap-2 flex-nowrap"">
                    <div class=""d-flex my-2"">
                      <button id=""btnNext"" class=""btn btn-primary"" onclick=""nextPage()"">
                        {(next == 0 ? "Back to Top" : "Next")}
                      </button>
                    </div>
                   <div class=""btn-group btn-group-sm me-2"" role=""group"">
                       <button class=""btn btn-outline-primary"" onclick=""toggleView('list')"">List View</button>
                       <button class=""btn btn-outline-secondary"" onclick=""toggleView('tree')"">Tree View</button>
                   </div>
                  <button type=""button"" class=""btn btn-success"" data-bs-toggle=""modal"" data-bs-target=""#insertModal"" title=""Add or Edit Key"">
                    " + Icons.KeyLg + @" Add/Edit
                  </button>
                  <div class=""btn-group btn-group-sm"" role=""group"" aria-label=""Page size"">
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size10"" onclick=""setSize(10)"">10</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size20"" onclick=""setSize(20)"">20</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size50"" onclick=""setSize(50)"">50</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size100"" onclick=""setSize(100)"">100</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size500"" onclick=""setSize(500)"">500</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size1000"" onclick=""setSize(1000)"">1000</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size5000"" onclick=""setSize(5000)"">5000</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size10000"" onclick=""setSize(10000)"">10000</button>
                  </div>
                </div>
              </div>
            </div>
          </div>";

        private static string BuildTable(string tbody) => $@"
          <div class=""container-fluid"">
                <div class=""table-responsive card shadow-sm"">
                  <table class=""table table-striped table-hover mb-0"" id=""redisTable"">
                    <thead class=""table-primary sticky-top"">
                      <tr>
                        <th>Type</th>
                        <th>Key</th>
                        <th>Size (KB)</th>
                        <th>TTL</th>
                        <th class=""text-center"">Action</th>
                      </tr>
                    </thead>
                    <tbody>
                      {tbody}
                    </tbody>
                  </table>
                </div>
              </div>";

        private static string BuildValuePanel() => @"
              <div class=""card shadow-sm h-100"">
                <div class=""card-header bg-info text-white d-flex justify-content-between align-items-center"">
                  <span>Value</span>
                  <select id=""jsonModeSelector"" class=""form-select form-select-sm w-auto"" onchange=""onViewerChange(this.value)"">                    
                    <option value=""view"">View</option>
                    <option value=""tree"">Tree</option>
                    <option value=""code"">Code</option>
                    <option value=""form"">Form</option>
                    <option value=""text"">Text</option>
                    <option value=""highlight"">Highlight.js</option>
                  </select>
                </div>
                <div class=""overflow-auto"" style=""max-height: 650px; min-height: 550px;"">
                  <!-- Contenedor para highlight.js -->
                  <pre id=""hljsContainer"" class=""mb-0 language-json d-none"" style=""white-space: pre-wrap; word-break: break-word;"">
                    <code id=""valueContent"">Select a key to view its value...</code>
                  </pre>

                  <!-- Contenedor para JSONEditor -->
                  <div id=""jsonEditorContainer"" style=""height: 100%;""></div>
                </div>
              </div>";


        private static string BuildScript(long next) => $@"
            document.addEventListener('DOMContentLoaded', function () {{
                initializeSearch();
                highlightActiveDbAndSize();
                addRowClickListeners();
                setupNextButton();
            }});

            document.addEventListener(""DOMContentLoaded"", function () {{
                document.querySelectorAll("".collapse"").forEach(function (el) {{
                    el.addEventListener(""shown.bs.collapse"", function () {{
                        const icon = document.querySelector(`#icon_${{el.id}}`);
                        if (icon) icon.textContent = ""📂"";
                    }});

                    el.addEventListener(""hidden.bs.collapse"", function () {{
                        const icon = document.querySelector(`#icon_${{el.id}}`);
                        if (icon) icon.textContent = ""📁"";
                    }});
                }});
            }});

            function initializeSearch() {{
                const params = new URLSearchParams(window.location.search);
                const db = params.get('db') || '0';
                const key = params.has('key') ? decodeURIComponent(params.get('key')) : '';
                const searchContainer = document.getElementById('search');
                searchContainer.innerHTML = '';
    
                const sInput = document.createElement('input');
                sInput.type = 'text';
                sInput.className = 'form-control';
                sInput.placeholder = 'key or pattern...';
                sInput.value = key;

                sInput.addEventListener('keydown', function(e) {{
                    if (e.key === 'Enter') {{
                        e.preventDefault();
                        showPage(0, db, sInput.value);
                    }}
                }});

                const sBtn = document.createElement('button');
                sBtn.innerText = 'Search';
                sBtn.className = 'btn btn-outline-success btn-sm ms-2';
                sBtn.onclick = () => showPage(0, db, sInput.value);

                searchContainer.append(sInput, sBtn);
            }}

            function toggleView(view) {{
              document.getElementById('listView').classList.toggle('d-none', view !== 'list');
              document.getElementById('treeView').classList.toggle('d-none', view !== 'tree');
            }}

            function highlightActiveDbAndSize() {{
                const params = new URLSearchParams(window.location.search);
                const db = params.get('db') || '0';
                document.getElementById('nav' + db)?.classList.add('active');
                document.getElementById('size' + (params.get('size') || '10'))?.classList.add('active');
            }}

            function addRowClickListeners() {{
                document.querySelectorAll('#redisTable tbody tr').forEach(row => {{
                    row.addEventListener('click', function () {{
                        const detail = JSON.parse(this.dataset.value);
                        renderDetailPanel(detail);
                    }});
                }});
            }}

            function setupNextButton() {{
                const btnNext = document.getElementById('btnNext');
                btnNext.onclick = () => nextPage();
                btnNext.hidden = ('0' === '{next}');
            }}

            function renderDetailPanel2(detail) {{
                const valueEl = document.getElementById('valueContent');
                if (!valueEl) return;
                valueEl.removeAttribute('data-highlighted');
                valueEl.className = 'language-json';
                valueEl.textContent = JSON.stringify(detail, null, 2);
                hljs.highlightElement(valueEl);
            }}
            
            let jsonEditorInstance;

            function onViewerChange(mode) {{
              // Mostrar/ocultar contenedores
              document.getElementById('hljsContainer').classList.toggle('d-none', mode !== 'highlight');
              document.getElementById('jsonEditorContainer').classList.toggle('d-none', mode === 'highlight');

              // Si cambian a JSONEditor, inicialízalo con el valor actual
              if (mode !== 'highlight' && window.currentDetail) {{
                ensureJsonEditor(mode);
                jsonEditorInstance.set(window.currentDetail);
              }}
            }}

            function renderDetailPanel(detail) {{
              window.currentDetail = detail; // guardamos para re-render con JSONEditor

              const mode = document.getElementById('jsonModeSelector').value;
              if (mode === 'highlight') {{
                // Highlight.js
                const codeEl = document.getElementById('valueContent');
                codeEl.textContent = JSON.stringify(detail, null, 2);
                hljs.highlightElement(codeEl);
              }} else {{
                // JSONEditor
                ensureJsonEditor(mode);
                jsonEditorInstance.set(detail);
              }}
            }}

            function ensureJsonEditor(mode) {{
              const container = document.getElementById('jsonEditorContainer');
              if (!jsonEditorInstance) {{
                jsonEditorInstance = new JSONEditor(container, {{
                  mode: mode,
                  mainMenuBar: true
                }});
              }} else {{
                jsonEditorInstance.setMode(mode);
              }}
            }}

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
                showPage({next}, new URLSearchParams(window.location.search).get('db') || '0', new URLSearchParams(window.location.search).get('key') || '');
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
        ";
    }
}
