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
                    <div class=""form-check mt-2"">
                        <input class=""form-check-input"" type=""checkbox"" id=""flushCheckbox"">
                        <label class=""form-check-label text-danger"" for=""flushCheckbox"">
                            Flush entire DB (⚠️ deletes all keys)
                        </label>
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
                const flushCheckbox = document.getElementById(""flushCheckbox"");
                const flush = flushCheckbox.checked;

                const urlParams = new URLSearchParams(window.location.search);
                if (!flush) {{
                    if (!pattern) {{
                        alert(""Please enter a pattern."");
                        return;
                    }}

                    if (isNaN(maxKeys) || maxKeys < 1 || maxKeys > 100000) {{
                        alert(""Please enter a valid number between 1 and 100000."");
                        return;
                    }}
                    urlParams.set(""key"", pattern);
                    urlParams.set(""size"", maxKeys.toString());
                }} else {{
                    urlParams.set(""flush"", ""true"");
                }}

                const spinner = document.getElementById(""deleteSpinner"");
                const deleteBtn = document.getElementById(""deleteBtn"");
                

                const db = urlParams.get(""db"") || ""0"";

                let confirmMsg = """";

                if (flush) {{
                    confirmMsg = `⚠️ This will delete ALL keys in the Redis DB ${{db}}.\nAre you sure?`;
                }} else {{
                    confirmMsg = `You are about to delete up to ${{maxKeys}} keys matching pattern in the Redis: \n DB: ${{db}} \n PATTERN: ""${{pattern}}""\nAre you sure?`;
                }}

                if (!confirm(confirmMsg)) return;

                spinner.classList.remove(""d-none"");
                deleteBtn.disabled = true;

                fetch(`${{API_PATH_BASE_URL}}/delete-by-pattern?${{urlParams.toString()}}`, {{
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

            document.getElementById(""flushCheckbox"").addEventListener(""change"", function () {{
                const disable = this.checked;
                document.getElementById(""patternInput"").disabled = disable;
                document.getElementById(""maxKeysInput"").disabled = disable;
            }});
        ";



    }

}
