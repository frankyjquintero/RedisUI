namespace RedisUI.Pages
{
    public static class Main
    {
        public static string BuildBase(RedisUISettings settings)
        {
            var html = $@"
                {InsertModal.Build()}
                {DeleteModal.Build()}
                {BuildHeader()}
                <div class=""row justify-content-start"">
                    <div class='col-8'>
                      <section class=""border-bottom pb-2 mb-3"">
                          {BuildToolbar()}
                      </section>
                      <div class=""container-fluid px-0"">
                        <div class=""card shadow-sm border-0"">
                            {BuildToolbar2()}
                            <div id='listView' class='d-none'>
                                {BuildBaseTable()}
                            </div>
                            <div id='treeView' class='custom-scroll'  style='max-height: 750px; overflow-y: auto;'>
                            </div>
                        </div>
                       </div>
                      </div>
                    <div class='col-4 custom-scroll'  style='max-height: 750px; overflow-y: auto;'>
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

        private static string BuildToolbar() => $@"
            <div class=""d-flex justify-content-between align-items-center mt-2"">
                <h6 class=""mb-0 fw-semibold d-flex align-items-center"">
                    <i class='bi bi-table me-1'></i> Redis Keys Overview
                </h6>                
                <div class=""btn-group btn-group-sm"" role=""group"" aria-label=""Page size"">                    
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size100"" onclick=""setSize(100)""><i class=""bi bi-ui-radios-grid me-1""></i> 100</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size500"" onclick=""setSize(500)""><i class=""bi bi-ui-radios-grid me-1""></i> 500</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size1000"" onclick=""setSize(1000)""><i class=""bi bi-ui-radios-grid me-1""></i> 1000</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size3000"" onclick=""setSize(3000)""><i class=""bi bi-ui-radios-grid me-1""></i> 3000</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size5000"" onclick=""setSize(5000)""><i class=""bi bi-ui-radios-grid me-1""></i> 5000</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size7000"" onclick=""setSize(7000)""><i class=""bi bi-ui-radios-grid me-1""></i> 7000</button>
                    <button type=""button"" class=""btn btn-outline-secondary"" id=""size10000"" onclick=""setSize(10000)""><i class=""bi bi-ui-radios-grid me-1""></i> 10000</button>
                </div>
            </div>
        ";

        private static string BuildToolbar2() => $@"
            <div class=""card-header bg-light border-bottom d-flex align-items-center justify-content-between flex-wrap gap-2"">

                <!-- View Mode Buttons -->
                <div class=""btn-group btn-group-sm"" role=""group"" aria-label=""View Mode"">
                    <button class=""btn btn-outline-primary"" id=""btnlistView"" onclick=""toggleView('list')"">
                        <i class=""bi bi-list-ul me-1""></i> List View
                    </button>
                    <button class=""btn btn-outline-secondary"" id=""btntreeView"" onclick=""toggleView('tree')"">
                        <i class=""bi bi-diagram-3 me-1""></i> Tree View
                    </button>
                </div>

                <!-- Bulk Action Buttons -->
                <div class=""btn-group btn-group-sm"" role=""group"" aria-label=""Page size"">
                    <button class=""btn btn-outline-danger"" onclick=""bulkDelete()"" title=""Delete selected keys"">
                        <i class=""bi bi-trash-fill me-1""></i> Bulk Delete
                    </button>
                    <button class=""btn btn-outline-warning"" onclick=""bulkExpire()"" title=""Expire selected keys"">
                        <i class=""bi bi-clock me-1""></i> Bulk Expire
                    </button>
                    <button class=""btn btn-outline-secondary"" onclick=""bulkRename()"" title=""Rename selected keys"">
                        <i class=""bi bi-pencil-square me-1""></i> Bulk Rename
                    </button>
                </div>

            </div>";



        #region HTML
        private static string BuildHeader() => $@"
          <div class=""container-fluid mb-3"">
            <div class=""row align-items-center"">

                <!-- Izquierda: Filtro y Búsqueda alineados horizontalmente -->
                <div class=""col-md-6"">
                    <div class=""d-flex align-items-center gap-2 w-100"">
    
                    <!-- Filtro por tipo -->
                    <div class=""input-group input-group-sm"" style=""width: 180px;"">
                        <span class=""input-group-text""><i class=""bi bi-funnel""></i></span>
                        <select id=""keyTypeFilter"" class=""form-select form-select-sm"" onchange=""showPage(0)"">
                        <option value="""">All</option>
                        <option value=""string"">String</option>
                        <option value=""list"">List</option>
                        <option value=""set"">Set</option>
                        <option value=""sortedset"">Sorted Set</option>
                        <option value=""hash"">Hash</option>
                        <option value=""stream"">Stream</option>
                        </select>
                    </div>

                    <!-- Búsqueda -->
                    <div class=""input-group input-group-sm flex-grow-1"">
                        <input type=""text"" id=""searchInput"" class=""form-control"" placeholder=""Key or pattern..."" 
                                onkeydown=""if(event.key === 'Enter') showPage(0);"">
                        <button class=""btn btn-outline-success"" onclick=""showPage(0)"" title=""Search"">
                        <i class=""bi bi-search""></i>
                        </button>
                        <button id=""btnNext"" class=""btn btn-primary"" onclick=""nextPage(0)"" title=""Next Page"">
                        <i class=""bi bi-chevron-right""></i>
                        </button>
                    </div>

                    </div>
                </div>

                <!-- Derecha: Acciones -->
                <div class=""col-md-6 d-flex justify-content-end align-items-center gap-2"">
                <div class=""btn-group btn-group-sm"" role=""group"" aria-label=""Key actions"">
                    <button type=""button"" class=""btn btn-outline-success"" data-bs-toggle=""modal"" data-bs-target=""#insertModal"" title=""Add or Edit Key"">
                    <i class=""bi bi-plus-circle""></i> Add
                    </button>
                    <button type=""button"" class=""btn btn-outline-danger"" data-bs-toggle=""modal"" data-bs-target=""#deletePatternModal"" title=""Delete by Pattern"">
                    <i class=""bi bi-trash""></i> Delete
                    </button>
                </div>
                </div>

            </div>
          </div>";




        private static string BuildBaseTable() => $@"
            <div class=""table-responsive"">
                <div class=""card border-0 shadow-sm"">
                    <div class=""table-wrapper custom-scroll"" style=""max-height: 600px; overflow-y: auto;"">
                        <table class=""table table-bordered table-hover align-middle mb-0 text-nowrap"">
                            <thead class=""table-primary text-center sticky-top"">
                                <tr>
                                    <th style=""width: 40px;"">
                                        <input type=""checkbox"" id=""selectAllKeys"" />
                                    </th>
                                    <th style=""width: 120px;"">Type</th>
                                    <th>Key</th>
                                    <th style=""width: 100px;"">Size (KB)</th>
                                    <th style=""width: 90px;"">TTL</th>
                                    <th style=""width: 110px;"">Action</th>
                                </tr>
                            </thead>
                            <tbody id=""tableBody"">
                                <!-- rows injected here -->
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>";


        private static string BuildValuePanel() => $@"
            <div class=""card shadow-sm h-100 border-0"">
              <div class=""card-header bg-danger text-white d-flex justify-content-between align-items-center"">
                <h6 class=""mb-0 d-flex align-items-center gap-2"">
                  <i class=""bi bi-box-seam""></i>
                  <span>Value:</span>
                  <code id=""currentKeyName"" class=""small text-white-50"">[none]</code>
                </h6>
                <select id=""jsonModeSelector"" class=""form-select form-select-sm w-auto"" onchange=""onViewerChange(this.value)"">
                  <option value=""view"">View</option>
                  <option value=""tree"">Tree</option>
                  <option value=""code"">Code</option>
                  <option value=""form"">Form</option>
                  <option value=""text"">Text</option>
                  <option value=""highlight"">Highlight.js</option>
                </select>
              </div>

              <div class=""card-body p-0"">
                <!-- Scrollable content area -->
                <div class=""overflow-auto custom-scroll"" style=""max-height: 650px; min-height: 650px;"">

                  <!-- Highlight.js container -->
                  <pre id=""hljsContainer"" class=""mb-0 language-json d-none"" style=""white-space: pre-wrap; word-break: break-word;"">
                    <code id=""valueContent"">Select a key to view its value...</code>
                  </pre>

                  <!-- JSON Editor container -->
                  <div id=""jsonEditorContainer"" style=""min-height: 650px;""></div>
                  <style>
                      #jsonEditorContainer .jsoneditor {{ border: 1px solid #dc3545 !important; background-color: #fff5f5 !important; }}
                      #jsonEditorContainer .jsoneditor-menu {{ background-color: #dc3545 !important; color: white !important; }}
                      #jsonEditorContainer .jsoneditor-selected {{ background-color: #f8d7da !important; }}
                    </style>
                </div>
              </div>
            </div>";

        #endregion HTML

        #region JS
        private static string BuildScript()
        {
            return $@"
                {BuildGlobalVariablesScript()}
                {BuildDOMContentLoaded()}
                {BuildNavScript()}
                {BuildJsHeader()}
                {BuildToolbarScript()}                               
                {BuildJsTable()}
                {BuildTreeView()}
                {BuildJsEditor()}
                {BuildScriptActions()}
            ";
        }

        private static string BuildToolbarScript()
        {
            return $@"
                function toggleView(view) {{
                    toggleViewActive = view;
                    const isList = view === 'list';
                    const isTree = view === 'tree';

                    document.getElementById('listView').classList.toggle('d-none', !isList);
                    document.getElementById('treeView').classList.toggle('d-none', !isTree);
                    document.getElementById('btnlistView').classList.toggle('active', isList);
                    document.getElementById('btntreeView').classList.toggle('active', isTree);

                    if (isList) {{
                        renderTableBody(currentDataKeys);
                    }} else if (isTree) {{
                        renderTreeView(currentDataKeys);
                    }}

                    setupNextButton(currentCursor);
                }}

                {BuildBulkActionsScript()} 
            ";
        }

        private static string BuildGlobalVariablesScript()
        {
            return $@"
                let isLoading = false;
                let currentDataKeys = [];
                let currentCursor = 0;
                let toggleViewActive = 'tree';
            ";
        }

        private static string BuildDOMContentLoaded() => $@"
            document.addEventListener('DOMContentLoaded', function () {{
                toggleView('tree');
                setupNextButton(0);
                highlightActiveDbAndSize();
            }});
        ";

        private static string BuildJsHeader() => $@"
            function highlightActiveDbAndSize() {{
                const params = new URLSearchParams(window.location.search);
                const db = params.get('db') || '0';
                const size = params.get('size') || '500';

                document.querySelectorAll('.dropdown-item').forEach(el => {{
                    el.classList.remove('bg-light', 'text-dark', 'fw-semibold');
                }});

                document.getElementById('nav' + db)?.classList.add('bg-light', 'text-dark', 'fw-semibold');
                document.getElementById('size' + size)?.classList.add('active');
                showPage(0, params.get('db'));
            }}


            function setupNextButton(cursor) {{
                const btnNext = document.getElementById('btnNext');
                btnNext.onclick = () => nextPage(cursor);
                btnNext.hidden = (0 === cursor);
            }}

            function nextPage(cursor) {{
                showPage(cursor, new URLSearchParams(window.location.search).get('db') || '0');
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

            function renderDetailPanel(key, detail) {{
              window.currentDetail = detail;
              window.currentKey = key;

              document.getElementById('currentKeyName').textContent = key || '[none]';

              const mode = document.getElementById('jsonModeSelector').value;

              if (mode === 'highlight') {{
                const codeEl = document.getElementById('valueContent');
                codeEl.textContent = JSON.stringify(detail, null, 2);
                hljs.highlightElement(codeEl);
              }} else {{
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

            function renderTableBody(keys) {{
              const tbody = document.getElementById(""tableBody"");
              tbody.innerHTML = """";

              keys.forEach(key => {{
                const tr = document.createElement(""tr"");
                tr.setAttribute(""data-value"", JSON.stringify(key.detail.value));
                tr.setAttribute(""data-key"", key.name);

                tr.innerHTML = `
                  <td><input type=""checkbox"" class=""keyCheckbox"" value=""${{key.name}}"" /></td> 
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

                tr.addEventListener(""click"", function (e) {{
                  if (e.target.closest(""input, button, a"")) return;

                  const value = this.getAttribute(""data-value"");
                  const key = this.getAttribute(""data-key"");

                  try {{
                    renderDetailPanel(key, JSON.parse(value));
                  }} catch (err) {{
                    console.error(""Invalid JSON in data-value"", err);
                  }}
                }});

                tbody.appendChild(tr);
              }});
            }}
        ";

        private static string BuildTreeView() => $@"

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

                    const checkbox = document.createElement(""input"");
                    checkbox.type = ""checkbox"";
                    checkbox.className = ""form-check-input me-2 keyCheckbox"";
                    checkbox.value = key.name;
                    checkbox.onclick = (e) => e.stopPropagation();

                    const badge = `<span class='badge ${{key.detail.badge}}'>${{key.detail.type.toUpperCase()}}</span>`;
                    detailSpan.innerHTML = `
                    <i class='bi bi-key-fill'></i> ${{badge}} ${{key.name}}
                    <small class=""text-muted"">
                        TTL: <span class=""badge bg-secondary"">${{key.detail.ttl ?? ""&#9854;""}}</span> |
                        Size: ${{key.detail.length}} KB
                    </small>`;

                    detailSpan.onclick = () => renderDetailPanel(detailSpan.dataset.key, JSON.parse(detailSpan.dataset.value));

                  
                    const deleteBtn = document.createElement(""a"");
                    deleteBtn.className = ""btn btn-sm btn-outline-danger"";
                    deleteBtn.innerHTML = ""<i class='bi bi-trash-fill'></i>"";
                    deleteBtn.onclick = () => confirmDelete(key.name);

                    const divContentLeft = document.createElement(""div"");
                    divContentLeft.className = ""d-flex align-items-center gap-2"";
                    divContentLeft.appendChild(checkbox);
                    divContentLeft.appendChild(detailSpan);

                    const divKey = document.createElement(""div"");
                    divKey.className = ""d-flex justify-content-between align-items-center w-100"";
                    divKey.appendChild(divContentLeft);
                    divKey.appendChild(deleteBtn);

                    li.appendChild(divKey);
                }}

                parentElement.appendChild(li);
              }}
            }}
        ";

        private static string BuildNavScript()
        {
            return $@"
                function setdb(db){{
                    var currentPath = window.location.href.replace(window.location.search, '');
                    window.location = currentPath.replace('#', '') + '?db=' + db;
                }}
            ";
        }

        private static string BuildBulkActionsScript()
        {
            return $@"
                document.addEventListener(""change"", function(e) {{
                  if (e.target.id === ""selectAllKeys"") {{
                    const all = document.querySelectorAll("".keyCheckbox"");
                    all.forEach(cb => cb.checked = e.target.checked);
                  }}
                }});

                function getSelectedKeys() {{
                  return Array.from(document.querySelectorAll("".keyCheckbox:checked""))
                              .map(cb => cb.value);
                }}

                function bulkDelete() {{
                  const keys = getSelectedKeys();
                  if (!keys.length) return alert(""No keys selected."");
                  if (!confirm(`Delete ${{keys.length}} keys?`)) return;

                  fetch(`${{API_PATH_BASE_URL}}/bulk-operation`, {{
                    method: 'POST',
                    headers: {{ ""Content-Type"": ""application/json"" }},
                    body: JSON.stringify({{ operation: ""Delete"", keys: keys }})
                  }}).then(r => r.text()).then(msg => {{
                    alert(msg);
                    showPage(0);
                  }});
                }}

                function bulkExpire() {{
                  const keys = getSelectedKeys();
                  if (!keys.length) return alert(""No keys selected."");
                  const ttl = prompt(""TTL en segundos:"");
                  if (!ttl || isNaN(ttl)) return alert(""TTL inválido."");

                  fetch(`${{API_PATH_BASE_URL}}/bulk-operation`, {{
                    method: 'POST',
                    headers: {{ ""Content-Type"": ""application/json"" }},
                    body: JSON.stringify({{ operation: ""Expire"", keys: keys, args: parseInt(ttl) }})
                  }}).then(r => r.text()).then(msg => {{
                    alert(msg);
                    showPage(0);
                  }});
                }}

                function bulkRename() {{
                  const keys = getSelectedKeys();
                  if (!keys.length) return alert(""No keys selected."");
                  const prefix = prompt(""Nuevo prefijo:"");
                  if (!prefix) return;

                  fetch(`${{API_PATH_BASE_URL}}/bulk-operation`, {{
                    method: 'POST',
                    headers: {{ ""Content-Type"": ""application/json"" }},
                    body: JSON.stringify({{ operation: ""Rename"", keys: keys, args: {{ prefix: prefix }} }})
                  }}).then(r => r.text()).then(msg => {{
                    alert(msg);
                    showPage(0);
                  }});
                }}
            ";
        }

        private static string BuildScriptActions() => $@"

            function showPage(cursor = 0, db = '0') {{
              if (isLoading) return;

              isLoading = true;
              showTableLoading();
              showTreeViewLoading();

              const params = new URLSearchParams();
              params.set('cursor', cursor);
              params.set('db', db);

              const activeSizeBtn = document.querySelector('.btn-group button.active');
              const size = activeSizeBtn ? activeSizeBtn.textContent : '500';
              params.set('size', size);

              const key = document.getElementById('searchInput')?.value || '';
              params.set('key', key);

              const type = document.getElementById('keyTypeFilter')?.value || '';
              params.set('type', type);

              fetch(`${{API_PATH_BASE_URL}}/keys?${{params.toString()}}`)
                .then(res => res.json())
                .then(data => {{
                  currentDataKeys = data.keys;
                  currentCursor = data.cursor;
                  toggleView(toggleViewActive);
                }})
                .catch(err => {{
                  console.error(""Error loading keys"", err);
                }})
                .finally(() => {{
                  isLoading = false;
                }});
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
        ";

        #endregion JS
    }
}
