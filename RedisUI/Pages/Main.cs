using RedisUI.Contents;
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
            var html = $@"
                {InsertModal.Build()}
                {BuildHeader()}
                {BuildTable(tbody, next)}
                {BuildValuePanel()}
                <script>
                {BuildScript(next)}
                </script>
            ";
            return html;
        }

        private static string BuildTableBody(List<KeyModel> keys)
        {
            var tbody = new StringBuilder();
            foreach (var key in keys)
            {
                var json = JsonSerializer.Serialize(key.Detail.Value);
                var columns = $"<td><span class=\"badge {key.Detail.Badge}\">{key.KeyType}</span></td><td>{key.Name}</td><td>{key.Detail.Length}</td><td>{(key.Detail.TTL.HasValue ? $"{key.Detail.TTL} s" : "∞")}</td>";
                tbody.Append($"<tr style=\"cursor: pointer;\" data-value='{json}' data-key='{key.Name}'>{columns}<td class=\"text-center\"><a onclick=\"confirmDelete('{key.Name}')\" class=\"btn btn-sm btn-outline-danger\"><span>{Icons.Delete}</span></a></td></tr>");
            }
            return tbody.ToString();
        }

        private static string BuildHeader() => @"
          <div class=""container-fluid"">
            <div class=""row align-items-center mb-3"">
              <div class=""col-sm-12 col-md-6"">
                <div id=""search"" class=""input-group""></div>
              </div>
              <div class=""col-sm-12 col-md-6"">
                <div class=""d-flex justify-content-end align-items-center gap-2 flex-nowrap"">
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
                  </div>
                </div>
              </div>
            </div>
          </div>";

        private static string BuildTable(string tbody, long next) => $@"
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
                        <th>TTL</th>
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
              </div>";

        private static string BuildValuePanel() => @"
              <div class=""col-lg-6"">
                <div class=""card shadow-sm h-100"">
                  <div class=""card-header bg-info text-white"">Value</div>
                  <div class=""card-body overflow-auto"">
                    <pre class=""mb-0""><code id=""valueContent"" class=""language-json"">Click on a key to get value...</code></pre>
                  </div>
                </div>
              </div>
            </div>
          </div>";

        private static string BuildScript(long next) => $@"
            document.addEventListener('DOMContentLoaded', function () {{
                initializeSearch();
                highlightActiveDbAndSize();
                addRowClickListeners();
                setupNextButton();
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
                const sBtn = document.createElement('button');
                sBtn.innerText = 'Search';
                sBtn.className = 'btn btn-outline-success btn-sm ms-2';
                sBtn.onclick = () => showPage(0, db, sInput.value);
                searchContainer.append(sInput, sBtn);
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

            function renderDetailPanel(detail) {{
                const valueEl = document.getElementById('valueContent');
                if (!valueEl) return;
                valueEl.removeAttribute('data-highlighted');
                valueEl.className = 'language-json';
                valueEl.textContent = JSON.stringify(detail, null, 2);
                hljs.highlightElement(valueEl);
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
