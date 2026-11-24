"""
Genera Scripts/03_seed_catalog.sql a partir de los Excel que pasó el cliente.

    python -I generate_catalog_seed.py <inventory-survey.xls> <FORMULARIOS DE PEDIDO.xlsx> <salida.sql>

- inventory-survey (.xls, levantamiento de inventario): catálogo completo con código, stock y costo.
- FORMULARIOS DE PEDIDO (.xlsx, la hojita): qué textos van en cada curso.

Los textos del formulario se cruzan por nombre con el inventario (coincidencia aproximada). Los que
no aparecen en el inventario (ej. Lectoescritura) se crean sin código. Al final imprime el reporte
del cruce para revisarlo a mano.
"""
import difflib
import re
import sys
import unicodedata

import openpyxl
import xlrd

INVENTORY, FORM, OUTPUT = sys.argv[1], sys.argv[2], sys.argv[3]
sys.stdout.reconfigure(encoding="utf-8")

# --------------------------------------------------------------------------- utilidades


def norm(text):
    text = unicodedata.normalize("NFKD", str(text)).encode("ascii", "ignore").decode().upper().strip()
    # "+ 4 MATEMÁTICA" (serie "Más") es otro libro que "MATEMÁTICA 4": el "+" inicial cuenta.
    text = re.sub(r"^\+", "MAS ", text)
    text = text.replace("CUDERNILLO", "CUADERNILLO").replace("PL-", "PLE-").replace("PL ", "PLE ")
    text = re.sub(r"[^A-Z0-9 ]+", " ", text)
    text = re.sub(r"\bPLE\b", " ", text)  # "PLE -" (plan lector) se escribe de mil formas
    return re.sub(r"\s+", " ", text).strip()


def sql(value):
    if value is None:
        return "NULL"
    if isinstance(value, bool):
        return "TRUE" if value else "FALSE"
    if isinstance(value, (int, float)):
        return repr(round(value, 2))
    return "'" + str(value).replace("'", "''") + "'"


LETTER_SIZES = ["XS", "S", "M", "L", "XL", "XXL", "2XL", "3XL", "4XL", "5XL"]
WORD_SIZES = {"PEQUEÑO": 1, "PEQUEÑA": 1, "MEDIANO": 2, "MEDIANA": 2, "GRANDE": 3}


def size_order(size):
    size = size.upper()
    if size in WORD_SIZES:
        return WORD_SIZES[size]
    if size in LETTER_SIZES:
        return 100 + (LETTER_SIZES.index(size) if size != "XXL" else LETTER_SIZES.index("2XL"))
    match = re.match(r"^(\d+)(?: 1/2)?$", size)
    if match:
        return int(match.group(1)) * 2 + (1 if "1/2" in size else 0)
    return 500


# --------------------------------------------------------------------------- inventario

products = []  # dicts
sheet = xlrd.open_workbook(INVENTORY).sheet_by_index(0)
group = None
for r in range(sheet.nrows):
    row = [sheet.cell_value(r, c) for c in range(sheet.ncols)]
    if row[1] == "Grupo Contable:":
        group = str(row[4]).strip()
        continue
    label = str(row[1]).strip()
    match = re.match(r"^(\d+) - (.+)$", label)
    if not match or not isinstance(row[11], float):
        continue
    code, name = match.group(1), match.group(2).strip()
    stock = row[10] if isinstance(row[10], float) else 0.0
    product = dict(code=code, name=name, group=group, stock=stock, cost=row[11],
                   section="OTHER", garment=None, size=None, size_order=0, active=True)

    number = group.split(" - ")[0]
    if number == "18":
        product["section"] = "TEXT"
    elif number == "16":
        product["section"] = "NOTEBOOK" if re.search(r"CUADERNO|HOJAS|TAPA CARPETA", name) else "OTHER"
        if name.startswith("AGENDA ESCOLAR") and stock <= 0:
            product["active"] = False  # agendas de años anteriores sin stock
    elif number in ("19", "50"):
        product["section"] = "UNIFORM"
        talla = re.match(r"^(.+?)\s+TALLA\s+(.+)$", name)
        if talla:
            product["garment"], product["size"] = talla.group(1).replace(" - ", " ").strip(), talla.group(2).strip()
        else:  # CORBATA GRANDE / MEDIANA / PEQUEÑA
            parts = name.rsplit(" ", 1)
            product["garment"], product["size"] = parts[0], parts[1] if len(parts) > 1 else "ÚNICA"
    elif number == "20" and re.match(r"^(CHALINA|GORRO|MITONES)", name):
        product["section"] = "UNIFORM"
        parts = name.split(" ", 1)
        product["garment"], product["size"] = parts[0], parts[1] if len(parts) > 1 else "ÚNICA"

    if product["size"]:
        product["size_order"] = size_order(product["size"])
    products.append(product)

texts = [p for p in products if p["section"] == "TEXT"]
text_index = {norm(p["name"]): p for p in texts}


def digits(tokens):
    return [t for t in tokens if t.isdigit()]


def best_match(key):
    """Cruce seguro: los números del libro deben coincidir (MY TIME 1 nunca es MY TIME 6) y un nombre
    debe contener al otro ("MY TIME 1" ⊂ "ENGLISH MY TIME 1"). Si no, un parecido muy alto con los
    mismos números. Devuelve (producto, cómo) o (None, None)."""
    form_tokens = key.split()
    form_set, form_digits = set(form_tokens), digits(form_tokens)
    best, best_score = None, 0.0
    for inv_key, product in text_index.items():
        inv_tokens = inv_key.split()
        inv_set, inv_digits = set(inv_tokens), digits(inv_tokens)
        if not inv_digits == form_digits[: len(inv_digits)] and not form_digits == inv_digits[: len(form_digits)]:
            continue
        if inv_set <= form_set or form_set <= inv_set:
            score = len(inv_set & form_set) / len(inv_set | form_set)
            if score > best_score:
                best, best_score = (product, inv_key), score
    if best:
        return best[0], f"contenido ({best[1]})"
    same_digits = [k for k in text_index if digits(k.split()) == form_digits]
    close = difflib.get_close_matches(key, same_digits, n=1, cutoff=0.86)
    if close:
        return text_index[close[0]], f"aprox. ({close[0]})"
    return None, None

# --------------------------------------------------------------------------- formulario: textos por curso

COURSE_LEVEL = [("PRE INICIAL", "INICIAL"), ("INICIAL", "INICIAL"), ("PRIMARIA", "PRIMARIA"), ("SECUNDARIA", "SECUNDARIA")]


def pretty_course(header):
    header = re.sub(r"\s+", " ", header).strip().upper()
    for key, level in COURSE_LEVEL:
        if header.endswith(key):
            return header.title().replace("Ro ", "ro ").replace("Do ", "do ").replace("To ", "to "), level
    raise ValueError(header)


courses = []  # (name, level, [item names])
workbook = openpyxl.load_workbook(FORM, data_only=True)
for ws in workbook.worksheets:
    if ws.title.upper() == "REVERSO":
        continue
    for header_row, last_row in ((9, 23), (25, 40)):
        header = ws.cell(header_row, 1).value
        if not header:
            continue
        name, level = pretty_course(str(header))
        items = []
        for r in range(header_row + 1, last_row + 1):
            value = ws.cell(r, 1).value
            if value and str(value).strip() and not str(value).strip().upper().startswith(("UNIFORMES", "TALLAS")):
                items.append(re.sub(r"\s+", " ", str(value)).strip())
        courses.append((name, level, items))

# --------------------------------------------------------------------------- cruce formulario ↔ inventario

report = []
new_products = []
course_items = []  # (course index, product ref, default selected)

agenda = dict(code=None, name="AGENDA ESCOLAR - ASEA", group="16 - VARIOS", stock=0.0, cost=None,
              section="OTHER", garment=None, size=None, size_order=0, active=True)
new_products.append(agenda)

for index, (course_name, level, items) in enumerate(courses):
    for item in items:
        key = norm(item)
        product = text_index.get(key)
        how = "exacto"
        if product is None:
            product, how = best_match(key)
        if product is None and "LECTOESCRITURA" in key:
            # Un libro distinto por curso aunque la hoja diga lo mismo en 1ro, 2do y 3ro.
            product = dict(code=None, name=f"{item.upper()} - {course_name.upper()}", group="18 - EDUCACES",
                           stock=0.0, cost=None, section="TEXT", garment=None, size=None, size_order=0, active=True)
            new_products.append(product)
            how = "nuevo (sin código, fuera del inventario)"
        elif product is None:
            product = next((p for p in new_products if norm(p["name"]) == key), None)
            if product is None:
                product = dict(code=None, name=item.upper(), group="18 - EDUCACES", stock=0.0, cost=None,
                               section="TEXT", garment=None, size=None, size_order=0, active=True)
                new_products.append(product)
                text_index[key] = product
            how = "NO ESTÁ EN EL INVENTARIO → creado sin código"
        course_items.append((index, product, True))
        report.append(f"{course_name:18} | {item[:60]:60} | {how}")
    course_items.append((index, agenda, False))  # agenda: opcional, desmarcada

# --------------------------------------------------------------------------- SQL

all_products = products + new_products
for number, product in enumerate(all_products, start=1):
    product["ref"] = number

lines = [
    "-- =====================================================================",
    "-- Catálogo inicial de BackToSchool. GENERADO por tools/generate_catalog_seed.py",
    "-- a partir del levantamiento de inventario y del formulario de pedido.",
    "-- No editar a mano: volver a generar. Los precios quedan en 0 hasta que",
    "-- el cliente los pase (se cargan desde la pantalla de Precios).",
    "-- =====================================================================",
    "",
    "BEGIN;",
    "",
    "-- Las 5 unidades educativas de la ASEA MBOS (orden en que aparecen al promotor).",
    "INSERT INTO school (name, display_order) VALUES",
    "('Patacamaya', 1), ('Viacha', 2), ('Shalom', 3), ('Reid Shepard', 4), ('Franz Tamayo', 5)",
    "ON CONFLICT (name) DO UPDATE SET display_order = EXCLUDED.display_order;",
    "",
]
for order, (name, level, _) in enumerate(courses, start=1):
    lines.append(f"INSERT INTO course (name, level, display_order) VALUES ({sql(name)}, {sql(level)}, {order}) "
                 "ON CONFLICT (name) DO NOTHING;")
lines.append("")

lines.append("INSERT INTO product (code, name, section, account_group, garment_name, size, size_order, price, unit_cost, stock, is_active) VALUES")
values = []
for p in all_products:
    values.append(f"({sql(p['code'])}, {sql(p['name'])}, {sql(p['section'])}, {sql(p['group'])}, {sql(p['garment'])}, "
                  f"{sql(p['size'])}, {p['size_order']}, 0, {sql(p['cost'])}, {sql(p['stock'])}, {sql(p['active'])})")
lines.append(",\n".join(values))
lines.append("ON CONFLICT DO NOTHING;")
lines.append("")

lines.append("-- Lista de textos de cada curso (orden del formulario). La agenda va al final y desmarcada.")
lines.append("INSERT INTO course_product (course_id, product_id, display_order, is_default_selected)")
lines.append("SELECT c.course_id, p.product_id, v.display_order, v.is_default_selected")
lines.append("FROM (VALUES")
rows, counters = [], {}
for course_index, product, selected in course_items:
    counters[course_index] = counters.get(course_index, 0) + 1
    rows.append(f"    ({sql(courses[course_index][0])}, {sql(product['code'])}, {sql(product['name'])}, "
                f"{counters[course_index]}, {sql(selected)})")
lines.append(",\n".join(rows))
lines.append(") AS v(course_name, code, product_name, display_order, is_default_selected)")
lines.append("JOIN course c ON c.name = v.course_name")
lines.append("JOIN product p ON (v.code IS NOT NULL AND p.code = v.code) OR (v.code IS NULL AND p.code IS NULL AND p.name = v.product_name)")
lines.append("ON CONFLICT (course_id, product_id) DO NOTHING;")
lines.append("")
lines.append("COMMIT;")

with open(OUTPUT, "w", encoding="utf-8") as handle:
    handle.write("\n".join(lines) + "\n")

sections = {}
for p in all_products:
    sections[p["section"]] = sections.get(p["section"], 0) + 1
print(f"Productos: {len(all_products)} {sections} | Cursos: {len(courses)} | Ítems en listas: {len(course_items)}")
print("\n".join(report))
