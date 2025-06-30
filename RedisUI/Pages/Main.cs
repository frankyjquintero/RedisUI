
namespace RedisUI.Pages
{
    public static class Main
    {
        public static string BuildBase(RedisUISettings settings)
        {
            var html = $@"
                {InsertModal.Build()}
                {BuildHeader()}
                <div class=""row justify-content-start"">
                    <div class='col-8' style='max-height: 750px; overflow-y: auto;'>
                      <div id='listView' class='d-none'>
                        {BuildBaseTable()}
                      </div>
                      <div id='treeView'>
                      </div>
                    </div>
                    <div class='col-4'  style='max-height: 750px; overflow-y: auto;'>
                     {BuildValuePanel()}
                    </div>
                </div>
                <script>
                    const API_PATH_BASE_URL = '{settings.Path}';
                    {BuildScript()}
                </script>
            ";

            return html;
        }

        #region HTML
        private static string BuildHeader() => $@"
          <div class=""container-fluid"">
            <div class=""row align-items-center mb-3"">
              <div class=""col-sm-12 col-md-6"">
                <div id=""search"" class=""input-group""></div>
              </div>
              <div class=""col-sm-12 col-md-6"">
                <div class=""d-flex justify-content-end align-items-center gap-2 flex-nowrap"">
                    <div class=""d-flex my-2"">
                      <button id=""btnNext"" class=""btn btn-primary"" onclick=""nextPage(0)"">
                        Next
                      </button>
                    </div>
                   <div class=""btn-group btn-group-sm me-2"" role=""group"">
                       <button class=""btn btn-outline-primary"" onclick=""toggleView('list')"">List View</button>
                       <button class=""btn btn-outline-secondary"" onclick=""toggleView('tree')"">Tree View</button>
                   </div>
                  <button type=""button"" class=""btn btn-success"" data-bs-toggle=""modal"" data-bs-target=""#insertModal"" title=""Add or Edit Key"">
                    <i class=""bi bi-key-fill""></i> Add/Edit
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

        private static string BuildBaseTable() => $@"
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
                    <tbody id='tableBody'>
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
        #endregion HTML

        #region JS
        private static string BuildScript()
        {
            return $@"
                {BuildDOMContentLoaded()}
                {BuildJsSearch()}
                {BuildJsHeader()}
                {BuildJsEditor()}
                {BuildJsTable()}
                {BuildTreeView()}
                {BuildScriptActions()}
            ";
        }

        private static string BuildDOMContentLoaded() => $@"
            document.addEventListener('DOMContentLoaded', function () {{
                initializeSearch();
                highlightActiveDbAndSize();
                setupNextButton(0);
            }});
        ";

        private static string BuildJsSearch() => $@"
            function initializeSearch() {{
                const params = new URLSearchParams(window.location.search);
                const db = params.get('db') || '0';
                const searchContainer = document.getElementById('search');
                searchContainer.innerHTML = '';
    
                const sInput = document.createElement('input');
                sInput.type = 'text';
                sInput.className = 'form-control';
                sInput.placeholder = 'key or pattern...';
                sInput.id = 'searchInput';
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
        ";

        private static string BuildJsHeader() => $@"
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

            function setupNextButton(cursor) {{
                const btnNext = document.getElementById('btnNext');
                btnNext.onclick = () => nextPage(cursor);
                btnNext.hidden = (0 === cursor);
            }}
        ";

        private static string BuildJsEditor() => $@"
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
        ";

        private static string BuildJsTable() => $@"
            function renderTableBody(keys) {{
              const tbody = document.getElementById(""tableBody"");
              tbody.innerHTML = """";

              keys.forEach(key => {{
                const tr = document.createElement(""tr"");
                tr.setAttribute(""data-value"", JSON.stringify(key.detail.value));
                tr.setAttribute(""data-key"", key.name);

                tr.innerHTML = `
                  <td><span class=""badge ${{key.detail.badge}}"">${{key.detail.type.toUpperCase()}}</span></td>
                  <td>${{key.name}}</td>
                  <td>${{key.detail.length}}</td>
                  <td>${{key.detail.ttl ? `${{key.detail.ttl}} s` : ""&#9854;""}}</td>
                  <td class=""text-center"">
                    <a onclick=""confirmDelete('${{key.name}}')"" class=""btn btn-sm btn-outline-danger"">
                      <i class='bi bi-trash-fill'></i>
                    </a>
                  </td>
                `;

                tr.addEventListener(""click"", () => renderDetailPanel(key.detail.value));
                tbody.appendChild(tr);
              }});
            }}
        ";

        private static string BuildTreeView() => $@"
            function renderTreeView(keys) {{
              const tree = buildTreeStructure(keys);
              const container = document.getElementById(""treeView"");
              container.innerHTML = """"; // limpiar
              const ul = document.createElement(""ul"");
              ul.className = ""list-group"";
              renderTreeRecursive(tree, ul);
              container.appendChild(ul);
            }}

            function buildTreeStructure(keys) {{
              const root = {{}};
              keys.forEach(key => {{
                const parts = key.name.split("":"");
                let node = root;
                for (const part of parts) {{
                  node.children = node.children || {{}};
                  node.children[part] = node.children[part] || {{}};
                  node = node.children[part];
                }}
                node.key = key;
              }});
              return root;
            }}

            function renderTreeRecursive(node, parentElement, prefix = """", depth = 0, indexRef = {{ i: 0 }}) {{
              if (!node.children) return;

              const childrenKeys = Object.keys(node.children).sort();

              for (const childName of childrenKeys) {{
                const childNode = node.children[childName];
                const fullPath = prefix ? `${{prefix}}:${{childName}}` : childName;
                const zebraClass = indexRef.i++ % 2 === 0 ? ""bg-light"" : ""bg-white"";

                const li = document.createElement(""li"");
                li.className = `list-group-item p-2 ${{zebraClass}}`;

                if (childNode.children) {{
                  const collapseId = `collapse_${{fullPath.replace(/:/g, ""_"")}}`;
                  const iconId = `icon_${{collapseId}}`;
                  const expanded = depth <= 0;

                  const divToggle = document.createElement(""div"");
                  divToggle.className = ""d-flex justify-content-between align-items-center"";
                  divToggle.setAttribute(""data-bs-toggle"", ""collapse"");
                  divToggle.setAttribute(""href"", `#${{collapseId}}`);
                  divToggle.setAttribute(""role"", ""button"");

                  divToggle.innerHTML = `
                    <a class=""fw-bold text-decoration-none text-dark"">
                      <span id='${{iconId}}' class='me-1'><i class='bi bi-chevron-right'></i> <i class='bi bi-folder'></i></span>${{childName}}
                    </a>`;

                  const collapseDiv = document.createElement(""div"");
                  collapseDiv.className = `collapse ms-3 ${{expanded ? ""show"" : """"}}`;
                  collapseDiv.id = collapseId;

                  const ul = document.createElement(""ul"");
                  ul.className = ""list-group list-group-flush"";
                  renderTreeRecursive(childNode, ul, fullPath, depth + 1, indexRef);
                  collapseDiv.appendChild(ul);

                  li.appendChild(divToggle);
                  li.appendChild(collapseDiv);
                  collapseDiv.addEventListener(""shown.bs.collapse"", () => {{
                    document.getElementById(iconId).innerHTML = ""<i class='bi bi-chevron-down'></i> <i class='bi bi-folder2-open'></i>"";
                  }});
                  collapseDiv.addEventListener(""hidden.bs.collapse"", () => {{
                    document.getElementById(iconId).innerHTML = ""<i class='bi bi-chevron-right'></i> <i class='bi bi-folder'></i>"";
                  }});
                }}

                if (childNode.key) {{
                  const key = childNode.key;

                  const detailSpan = document.createElement(""span"");
                  detailSpan.className = ""text-break"";
                  detailSpan.style.cursor = ""pointer"";
                  detailSpan.dataset.key = key.name;
                  detailSpan.dataset.value = JSON.stringify(key.detail.value);

                  const badge = `<span class='badge ${{key.detail.badge}}'>${{key.detail.type.toUpperCase()}}</span>`;
                  detailSpan.innerHTML = `
                    <i class='bi bi-key-fill'></i> ${{badge}} ${{key.name}}
                    <small class=""text-muted"">
                      TTL: <span class=""badge"">${{key.detail.ttl ?? ""&#9854;""}}</span> |
                      Size: ${{key.detail.length}} KB
                    </small>`;

                  detailSpan.onclick = () => renderDetailPanel(JSON.parse(detailSpan.dataset.value));

                  const deleteBtn = document.createElement(""a"");
                  deleteBtn.className = ""btn btn-sm btn-outline-danger"";
                  deleteBtn.innerHTML = ""<i class='bi bi-trash-fill'></i>"";
                  deleteBtn.onclick = () => confirmDelete(key.name);

                  const divKey = document.createElement(""div"");
                  divKey.className = ""d-flex justify-content-between align-items-center"";
                  divKey.appendChild(detailSpan);
                  divKey.appendChild(deleteBtn);

                  li.appendChild(divKey);
                }}

                parentElement.appendChild(li);
              }}
            }}
        ";

        private static string BuildScriptActions() => $@"

            let isLoading = false;

            function showPage(cursor = 0, db = '0') {{
              if (isLoading) return;

              isLoading = true;
              showTableLoading();
              showTreeViewLoading();

              const params = new URLSearchParams();
              params.set('cursor', cursor);
              params.set('db', db);

              const activeSizeBtn = document.querySelector('.btn-group button.active');
              const size = activeSizeBtn ? activeSizeBtn.textContent : '10';
              params.set('size', size);

              const key = document.getElementById('searchInput')?.value || '';
              params.set('key', key);

              fetch(`${{API_PATH_BASE_URL}}/keys?${{params.toString()}}`)
                .then(res => res.json())
                .then(data => {{
                  renderTableBody(data.keys);
                  renderTreeView(data.keys);
                  setupNextButton(data.cursor);
                }})
                .catch(err => {{
                  console.error(""Error loading keys"", err);
                }})
                .finally(() => {{
                  isLoading = false; // Liberar bloqueo al terminar
                }});
            }}

            function showTableLoading() {{
              const tbody = document.getElementById(""tableBody"");
              if (tbody) {{
                tbody.innerHTML = `
                  <tr>
                    <td colspan=""5"" class=""text-center"">
                      <div class=""spinner-border text-primary me-2"" role=""status"" style=""width: 1.2rem; height: 1.2rem;"">
                        <span class=""visually-hidden"">Loading...</span>
                      </div>
                      Loading keys...
                    </td>
                  </tr>
                `;
              }}
            }}

            function showTreeViewLoading() {{
              const container = document.getElementById(""treeView"");
              if (container) {{
                container.innerHTML = `
                  <div class=""text-center text-muted p-3"">
                    <div class=""spinner-border text-success me-2"" role=""status"" style=""width: 1.2rem; height: 1.2rem;"">
                      <span class=""visually-hidden"">Loading...</span>
                    </div>
                    Loading key tree...
                  </div>
                `;
              }}
            }}

            function nextPage(cursor) {{
                showPage(cursor, new URLSearchParams(window.location.search).get('db') || '0');
            }}

            function setdb(db){{
                var currentPath = window.location.href.replace(window.location.search, '');
                window.location = currentPath.replace('#', '') + '?db=' + db;
            }}

            function setSize(size) {{
                document.querySelectorAll('.btn-group button').forEach(btn => {{
                    btn.classList.remove('active');
                }});

                const activeBtn = document.getElementById('size' + size);
                if (activeBtn) {{
                    activeBtn.classList.add('active');
                }}
                const url = new URL(window.location);
                const params = url.searchParams;
                if (!params.has('db')) {{
                    params.set('db', '0');
                }}

                showPage(0, params.get('db'));
            }}

            function confirmDelete(delKey) {{
                if (!confirm(`Are you sure to delete key '${{delKey}}'?`)) return;

                fetch(`${{API_PATH_BASE_URL}}/keys/${{encodeURIComponent(delKey)}}`, {{
                    method: 'DELETE'
                }})
                .then(response => {{                    
                    if (response.status != 204) throw new Error('Failed to delete key');
                    const url = new URL(window.location);
                    const params = url.searchParams;
                    if (!params.has('db')) {{
                        params.set('db', '0');
                    }}
                    showPage(0, params.get('db'));
                }})
                .catch(err => {{
                    console.error(""Error deleting key:"", err);
                    alert(""An error occurred while deleting the key."");
                }});
            }}


            function saveKey() {{
              const key = document.getElementById(""insertKey"").value.trim();
              const type = document.getElementById(""insertType"").value.trim().toLowerCase();
              const rawValue = document.getElementById(""insertValue"").value.trim();

              if (!key || !type || !rawValue) {{
                alert(""Please fill in all fields."");
                return;
              }}

              let parsedValue;
              try {{
                parsedValue = JSON.parse(rawValue);
              }} catch (err) {{
                alert(""Value must be valid JSON."");
                return;
              }}

              const payload = {{
                name: key,
                keyType: type,
                value: parsedValue
              }};

              fetch(`${{API_PATH_BASE_URL}}/keys`, {{
                method: ""POST"",
                headers: {{
                  ""Content-Type"": ""application/json""
                }},
                body: JSON.stringify(payload)
              }})
                .then(response => {{
                  if (response.status != 201) throw new Error(""Failed to save key"");
                  alert(""Key saved successfully."");
                  document.getElementById(""insertForm"").reset();
                  const modal = bootstrap.Modal.getInstance(document.getElementById(""insertModal""));
                  modal.hide();
                  window.location.reload();
                }})
                .catch(err => {{
                  console.error(""Save failed:"", err);
                  alert(""Error saving key."");
                }});
            }}


            function checkRequired() {{
                const k = document.getElementById('insertKey').value;
                const v = document.getElementById('insertValue').value;
                document.getElementById('btnSave').disabled = !(k && v);
            }}
        ";

        #endregion JS
    }
}
