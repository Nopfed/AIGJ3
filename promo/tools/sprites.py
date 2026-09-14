import re, os
SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "src", "SpiceWizard.Web", "Art", "Sprites.cs")
def load():
    txt = open(SRC, encoding="utf-8").read()
    out = {}
    for m in re.finditer(r'\["(\w+)"\]\s*=\s*new\[\]\s*\{(.*?)\}', txt, re.S):
        rows = re.findall(r'"([^"]*)"', m.group(2))
        out[m.group(1)] = rows
    return out
if __name__ == "__main__":
    s = load()
    for k, v in s.items():
        print(k, len(v[0]), len(v), set(len(r) for r in v) == {len(v[0])})
