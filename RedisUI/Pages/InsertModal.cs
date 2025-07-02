namespace RedisUI.Pages
{
    public static class InsertModal
    {
        public static string Build()
        {

            return $@"
                <!-- Insert Modal -->
                <div class=""modal fade"" id=""insertModal"" tabindex=""-1"" aria-labelledby=""insertModalLabel"" aria-hidden=""true"">
                  <div class=""modal-dialog modal-lg modal-dialog-scrollable"">
                    <div class=""modal-content"">
                      <div class=""modal-header bg-success text-white"">
                        <h5 class=""modal-title"" id=""insertModalLabel"">Insert Redis Key</h5>
                        <button type=""button"" class=""btn-close"" data-bs-dismiss=""modal"" aria-label=""Close""></button>
                      </div>

                      <div class=""modal-body"">
                        <form id=""insertForm"">
                          <div class=""mb-3"">
                            <label for=""insertKey"" class=""form-label"">Key Name</label>
                            <input type=""text"" class=""form-control"" id=""insertKey"" required>
                          </div>

                          <div class=""mb-3"">
                            <label for=""insertType"" class=""form-label"">Data Type</label>
                            <select class=""form-select"" id=""insertType"" onchange=""onTypeChange()"">
                              <option value=""string"">String</option>
                              <option value=""list"">List</option>
                              <option value=""set"">Set</option>
                              <option value=""sortedset"">SortedSet</option>
                              <option value=""hash"">Hash</option>
                              <option value=""stream"">Stream</option>
                            </select>
                          </div>
                          <div class=""mb-3"">
                            <label for=""insertTTL"" class=""form-label"">TTL (seconds, optional)</label>
                            <input type=""number"" min=""0"" class=""form-control"" id=""insertTTL"" placeholder=""Leave empty for no expiration"">
                          </div>
                          <div class=""mb-3"">
                            <label for=""insertValue"" class=""form-label"">Value (JSON format)</label>
                            <textarea class=""form-control"" id=""insertValue"" rows=""5"" required></textarea>
                            <div class=""form-text"">Example for list: [""a"",""b""] — for hash: {{""a"":""b""}}</div>
                          </div>

                        </form>
                      </div>

                      <div class=""modal-footer"">
                        <button type=""button"" class=""btn btn-secondary"" data-bs-dismiss=""modal"">Cancel</button>
                        <button type=""button"" class=""btn btn-success"" onclick=""saveKey()"">Save</button>
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
            function saveKey() {{
              const key = document.getElementById(""insertKey"").value.trim();
              const type = document.getElementById(""insertType"").value.trim().toLowerCase();
              const rawValue = document.getElementById(""insertValue"").value.trim();
              const ttlInput = document.getElementById(""insertTTL"");
              const ttl = ttlInput && ttlInput.value ? parseInt(ttlInput.value) : null;        

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
                value: parsedValue,
                ttl: ttl
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

            function onTypeChange() {{
              const type = document.getElementById(""insertType"").value;
              const valueInput = document.getElementById(""insertValue"");
              const valueHint = document.getElementById(""valueHint"");

              switch (type) {{
                case ""string"":
                  valueInput.placeholder = `""hello world""`;
                  valueHint.textContent = `Example for string: ""hello world""`;
                  break;

                case ""list"":
                  valueInput.placeholder = `[""item1"", ""item2"", ""item3""]`;
                  valueHint.textContent = `Example for list: [""item1"", ""item2""]`;
                  break;

                case ""set"":
                  valueInput.placeholder = `[""itemA"", ""itemB""]`;
                  valueHint.textContent = `Example for set: [""itemA"", ""itemB""] (duplicates ignored)`;
                  break;

                case ""sortedset"":
                  valueInput.placeholder = `[{{""score"": 1, ""member"": ""a""}}, {{""score"": 2, ""member"": ""b""}}]`;
                  valueHint.textContent = `Example for sorted set: [{{""score"": 1, ""member"": ""a""}}]`;
                  break;

                case ""hash"":
                  valueInput.placeholder = `{{""field1"": ""value1"", ""field2"": ""value2""}}`;
                  valueHint.textContent = `Example for hash: {{""name"": ""Alice"", ""age"": 30}}`;
                  break;

                case ""stream"":
                  valueInput.placeholder = `[{{""id"": ""*"", ""fields"": {{""foo"": ""bar""}}}}]`;
                  valueHint.textContent = `Example for stream: [{{""id"": ""*"", ""fields"": {{""foo"": ""bar""}}}}]`;
                  break;

                default:
                  valueInput.placeholder = """";
                  valueHint.textContent = """";
              }}
            }}

        ";
    }
}
