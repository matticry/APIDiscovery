import sys
import pyodbc
import pandas as pd
from PyQt5.QtWidgets import (QApplication, QMainWindow, QTableWidget,
                             QTableWidgetItem, QPushButton, QVBoxLayout,
                             QWidget, QHeaderView, QMessageBox, QLabel)
from PyQt5.QtCore import Qt

class SQLServerViewer(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Visor de Reportes")
        self.setGeometry(100, 100, 800, 600)

        # Crear widget central
        central_widget = QWidget()
        self.setCentralWidget(central_widget)

        # Crear layout
        layout = QVBoxLayout(central_widget)

        # Crear etiqueta de estado
        self.status_label = QLabel("Estado: Listo")
        layout.addWidget(self.status_label)

        # Crear tabla
        self.table = QTableWidget()
        layout.addWidget(self.table)

        # Crear botón de actualización
        self.refresh_button = QPushButton("Actualizar Datos")
        self.refresh_button.clicked.connect(self.load_data)
        layout.addWidget(self.refresh_button)

        # Configuración de conexión a SQL Server
        self.connection_string = (
            "Driver={ODBC Driver 17 for SQL Server};"
            "Server=207.246.69.9;"
            "Database=Ejemplo;"
            "User Id=infoelect;"
            "Password=vcsnfaM$1;"
            "TrustServerCertificate=True;"
        )

        # Cargar datos inicialmente
        self.load_data()

    def load_data(self):
        try:
            self.status_label.setText("Estado: Conectando a la base de datos...")
            QApplication.processEvents()

            # Conectar a SQL Server
            conn = pyodbc.connect(self.connection_string)

            # Consultar datos
            self.status_label.setText("Estado: Obteniendo datos...")
            QApplication.processEvents()

            query = "SELECT id_re, action_re, created_at_re, user_re, dni_re, status_re FROM tbl_report"
            df = pd.read_sql(query, conn)

            # Configurar la tabla
            self.table.setRowCount(len(df))
            self.table.setColumnCount(len(df.columns))
            self.table.setHorizontalHeaderLabels(df.columns)

            # Llenar la tabla con datos
            for row in range(len(df)):
                for col in range(len(df.columns)):
                    value = str(df.iloc[row, col])
                    item = QTableWidgetItem(value)
                    self.table.setItem(row, col, item)

            # Ajustar el ancho de las columnas
            self.table.horizontalHeader().setSectionResizeMode(QHeaderView.Stretch)

            # Cerrar conexión
            conn.close()

            self.status_label.setText(f"Estado: Datos cargados exitosamente. Registros: {len(df)}")

        except Exception as e:
            QMessageBox.critical(self, "Error", f"Error al cargar los datos: {str(e)}")
            self.status_label.setText(f"Estado: Error - {str(e)}")

def main():
    app = QApplication(sys.argv)
    window = SQLServerViewer()
    window.show()
    sys.exit(app.exec_())

if __name__ == "__main__":
    main()