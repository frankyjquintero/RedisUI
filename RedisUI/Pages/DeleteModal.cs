namespace RedisUI.Pages
{
    public class DeleteModal
    {
        public static string Build()
        {
            return $@"
            <!-- Delete Pattern Modal -->
            <div class=""modal fade"" id=""deletePatternModal"" tabindex=""-1"" aria-labelledby=""deletePatternModalLabel"" aria-hidden=""true"">
              <div class=""modal-dialog"">
                <div class=""modal-content"">
                  <div class=""modal-header bg-danger text-white"">
                    <h5 class=""modal-title"" id=""deletePatternModalLabel"">Delete Keys by Pattern</h5>
                    <button type=""button"" class=""btn-close"" data-bs-dismiss=""modal"" aria-label=""Close""></button>
                  </div>
                  <div class=""modal-body"">
                    <div class=""mb-3"">
                      <label for=""patternInput"" class=""form-label"">Enter pattern (e.g. <code>user:*</code>)</label>
                      <input type=""text"" class=""form-control"" id=""patternInput"" placeholder=""key:*"" required>
                    </div>
                    <div class=""mb-3"">
                      <label for=""maxKeysInput"" class=""form-label"">Max keys to delete <small>(max 100,000)</small></label>
                      <input type=""number"" class=""form-control"" id=""maxKeysInput"" placeholder=""1000"" value=""1000"" min=""1"" max=""100000"">
                    </div>
                  </div>
                  <div class=""modal-footer"">
                    <span id=""deleteSpinner"" class=""me-auto text-danger d-none"">
                      <span class=""spinner-border spinner-border-sm"" role=""status"" aria-hidden=""true""></span>
                      Deleting keys...
                    </span>
                    <button type=""button"" class=""btn btn-secondary"" data-bs-dismiss=""modal"">Cancel</button>
                    <button type=""button"" id=""deleteBtn"" class=""btn btn-danger"" onclick=""deleteByPattern()"">Delete</button>
                  </div>
                </div>
              </div>
            </div>
            <script>
            {BuildScriptJs()}
            </script>
            ";
        }
        private static string BuildScriptJs() => $@"
            function deleteByPattern() {{
                const pattern = document.getElementById(""patternInput"").value.trim();
                const maxKeys = parseInt(document.getElementById(""maxKeysInput"").value, 10);
                const spinner = document.getElementById(""deleteSpinner"");
                const deleteBtn = document.getElementById(""deleteBtn"");

                if (!pattern) {{
                    alert(""Please enter a pattern."");
                    return;
                }}

                if (isNaN(maxKeys) || maxKeys < 1 || maxKeys > 100000) {{
                    alert(""Please enter a valid number between 1 and 100000."");
                    return;
                }}

                // Obtener 'db' desde la URL (si está presente)
                const urlParams = new URLSearchParams(window.location.search);
                const db = urlParams.get(""db"") || ""0"";

                // Confirmación antes de ejecutar
                const confirmed = confirm(`You are about to delete up to ${{maxKeys}} keys matching pattern: ""${{pattern}}"" from DB: ${{db}}.\n\nAre you sure?`);
                if (!confirmed) return;

                const params = new URLSearchParams();
                params.set(""key"", pattern);
                params.set(""size"", maxKeys.toString());
                params.set(""db"", db);

                spinner.classList.remove(""d-none"");
                deleteBtn.disabled = true;

                fetch(`${{API_PATH_BASE_URL}}/delete-by-pattern?${{params.toString()}}`, {{
                    method: ""POST""
                }})
                .then(res => {{
                    if (res.status === 204) {{
                        alert(""No matching keys found to delete."");
                        return null;
                    }}
                    if (res.ok) return res.text();
                    throw new Error(""Failed to delete keys."");
                }})
                .then(msg => {{
                    if (msg) {{
                        alert(msg);
                        location.reload();
                    }}
                }})
                .catch(err => {{
                    console.error(err);
                    alert(""Error deleting keys"");
                }})
                .finally(() => {{
                    spinner.classList.add(""d-none"");
                    deleteBtn.disabled = false;
                }});
            }}
        ";


    }

}
