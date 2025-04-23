#!/usr/bin/env python3
"""
validate_invoice.py

Valida la firma XML-DSIG de una factura electrónica.
Dependencias: lxml, xmlsec
Instalación:
    pip install lxml xmlsec
Uso:
    python validate_invoice.py factura.xml
"""

import sys
from lxml import etree
import xmlsec

def verify_signature(xml_path):
    # Parsear el XML sin alterar espacios en blanco
    parser = etree.XMLParser(remove_blank_text=False)
    doc = etree.parse(xml_path, parser)

    # Registrar el atributo 'id' para la referencia
    xmlsec.tree.add_ids(doc, ["id"])

    # Buscar el nodo Signature
    signature_node = xmlsec.tree.find_node(doc, xmlsec.Node.SIGNATURE)
    if signature_node is None:
        print("ERROR: No se encontró el nodo <Signature>")
        return False

    # Crear contexto de firma
    ctx = xmlsec.SignatureContext()

    # Extraer el certificado X509 dentro de KeyInfo
    key_info_node = xmlsec.tree.find_node(signature_node, xmlsec.Node.KEYINFO)
    x509_node = xmlsec.tree.find_node(key_info_node, xmlsec.Node.X509CERTIFICATE)
    if x509_node is None or not x509_node.text:
        print("ERROR: No se encontró el certificado X509 en KeyInfo")
        return False

    # Formatear certificado PEM
    cert_pem = "-----BEGIN CERTIFICATE-----\n" + x509_node.text.strip() + "\n-----END CERTIFICATE-----\n"

    # Cargar la clave pública desde el certificado
    key = xmlsec.Key.from_memory(cert_pem, xmlsec.KeyFormat.CERT_PEM, None)
    ctx.key = key

    # Verificar la firma
    try:
        ctx.verify(signature_node)
        print("✅ Firma válida: la factura no ha sido alterada.")
        return True
    except xmlsec.Error as e:
        print(f"❌ Firma NO válida: {e}")
        return False

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Uso: python validate_invoice.py <ruta_factura.xml>")
        sys.exit(1)

    xml_file = sys.argv[1]
    ok = verify_signature(xml_file)
    sys.exit(0 if ok else 1)
