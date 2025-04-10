import pyodbc
import os
import time
from tqdm import tqdm

# Configuración de la conexión a SQL Server
connection_string = (
    "DRIVER={ODBC Driver 17 for SQL Server};"
    "SERVER=156.244.32.23;"
    "DATABASE=DB_Health;"
    "UID=sa;"
    "PWD=Toyotaro@12;"
    "TrustServerCertificate=yes;"
)

# Ruta donde se generarán los modelos
OUTPUT_DIR = r"C:\\Users\\matticry\\RiderProjects\\APIDiscovery\\APIDiscovery\\Models"

# Mapeo de tipos SQL a C#
SQL_TO_CSHARP = {
    'int': 'int',
    'bigint': 'long',
    'smallint': 'short',
    'tinyint': 'byte',
    'bit': 'bool',
    'decimal': 'decimal',
    'numeric': 'decimal',
    'money': 'decimal',
    'float': 'double',
    'real': 'float',
    'datetime': 'DateTime',
    'smalldatetime': 'DateTime',
    'date': 'DateTime',
    'time': 'TimeSpan',
    'char': 'string',
    'varchar': 'string',
    'text': 'string',
    'nchar': 'string',
    'nvarchar': 'string',
    'ntext': 'string'
}

def get_connection():
    return pyodbc.connect(connection_string)

def list_tables():
    with get_connection() as conn:
        cursor = conn.cursor()
        cursor.execute("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
        tables = [row[0] for row in cursor.fetchall()]
        return tables

def get_columns(table_name):
    with get_connection() as conn:
        cursor = conn.cursor()
        cursor.execute(f"""
            SELECT c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH,
                   kcu.COLUMN_NAME AS PK
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
            ON c.COLUMN_NAME = kcu.COLUMN_NAME AND c.TABLE_NAME = kcu.TABLE_NAME
            WHERE c.TABLE_NAME = '{table_name}'
        """)
        return cursor.fetchall()

def get_foreign_keys(table_name):
    with get_connection() as conn:
        cursor = conn.cursor()
        cursor.execute(f"""
            SELECT 
                f.name AS FK_name,
                COL_NAME(fc.parent_object_id,fc.parent_column_id) AS FK_column,
                OBJECT_NAME (f.referenced_object_id) AS referenced_table,
                COL_NAME(fc.referenced_object_id,fc.referenced_column_id) AS referenced_column
            FROM 
                sys.foreign_keys AS f
            INNER JOIN 
                sys.foreign_key_columns AS fc 
                ON f.OBJECT_ID = fc.constraint_object_id
            WHERE f.parent_object_id = OBJECT_ID('{table_name}')
        """)
        return cursor.fetchall()

def map_sql_to_csharp(sql_type):
    return SQL_TO_CSHARP.get(sql_type, 'string')

def pascal_case(name):
    # Elimina prefijos como 'tbl_', 't_', etc.
    if name.lower().startswith('tbl_'):
        name = name[4:]
    elif name.lower().startswith('t_'):
        name = name[2:]
    return ''.join(word.capitalize() for word in name.split('_'))

def generate_model(table):
    columns = get_columns(table)
    foreign_keys = get_foreign_keys(table)

    lines = [
        "using System.ComponentModel.DataAnnotations;",
        "using System.ComponentModel.DataAnnotations.Schema;",
        "using System.Text.Json.Serialization;",
        "",
        "namespace APIDiscovery.Models;",
        "",
        f"[Table(\"{table}\")]",
        f"public class {pascal_case(table)}",
        "{"
    ]

    fk_columns = {fk.FK_column: fk for fk in foreign_keys}

    for col in columns:
        col_name, sql_type, max_len, pk = col
        csharp_type = map_sql_to_csharp(sql_type)

        if pk:
            lines.append("    [Key]")
        if max_len and csharp_type == 'string':
            lines.append(f"    [MaxLength({max_len})]")

        lines.append(f"    public {csharp_type}{'?' if 'null' in sql_type else ''} {col_name} {{ get; set; }}")
        lines.append("")

    # Relaciones
    for fk in foreign_keys:
        ref_table = pascal_case(fk.referenced_table)
        lines.append(f"    [ForeignKey(\"{fk.FK_column}\")]\n    [JsonIgnore]\n    public {ref_table} {ref_table} {{ get; set; }}")

    lines.append("}")

    file_path = os.path.join(OUTPUT_DIR, f"{pascal_case(table)}.cs")
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(lines))

    print(f"\n✅ Modelo generado: {file_path}\n")

def main():
    while True:
        print("""
--- GENERADOR DE MODELOS C# ---
1. Generar modelos
2. Consultar tablas
3. Salir
""")
        opcion = input("Elige una opción: ")

        if opcion == '1':
            tablas = list_tables()
            print("\nTablas disponibles:")
            for i, t in enumerate(tablas):
                print(f"{i + 1}. {t}")

            idx = int(input("\nSelecciona la tabla que quieres generar: ")) - 1
            tabla = tablas[idx]

            print(f"\n⏳ Generando modelo para '{tabla}'...")
            for _ in tqdm(range(100), desc="Procesando"):
                time.sleep(0.01)
            generate_model(tabla)

        elif opcion == '2':
            tablas = list_tables()
            print("\nTablas disponibles:")
            for t in tablas:
                print(f"- {t}")

        elif opcion == '3':
            print("Saliendo...")
            break
        else:
            print("Opcion no válida. Intenta de nuevo.")

if __name__ == '__main__':
    main()