// lib/presentation/screens/create_invoice_screen.dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../config/theme/app_colors.dart';
import '../providers/emission_point_provider.dart';
import '../../data/services/enterprise_storage_service.dart';
import '../../data/services/article_service.dart';
import '../../core/api/api_client.dart';
import '../../domain/entities/article.dart';
import 'client_form_screen.dart';

class CreateInvoiceScreen extends StatefulWidget {
  @override
  _CreateInvoiceScreenState createState() => _CreateInvoiceScreenState();
}

class _CreateInvoiceScreenState extends State<CreateInvoiceScreen> {
  final _storageService = EnterpriseStorageService();
  int _currentStep = 0;
  bool _isInitialized = false;
  bool _isLoadingArticles = false;
  List<Article> _articles = [];
  bool _isSearchingClient = false;
  String _clientErrorMessage = '';
  int? _selectedClientId;
  String _searchQuery = '';
  List<Map<String, dynamic>> _selectedArticles = [];

  // Cliente
  bool _isFinalConsumer = true;
  final _clientIdController = TextEditingController();
  final _clientNameController = TextEditingController();
  final _clientAddressController = TextEditingController();
  final _clientEmailController = TextEditingController();

  final _tipController = TextEditingController(text: "");
  final _messageController = TextEditingController();

  // Controladores adicionales para validación
  final _formKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    _clientIdController.text = '9999999999999';
    _clientNameController.text = 'CONSUMIDOR FINAL';
    _clientAddressController.text = 'S/N';
    _clientEmailController.text = '';
  }

  @override
  void dispose() {
    _clientIdController.dispose();
    _clientNameController.dispose();
    _clientAddressController.dispose();
    _clientEmailController.dispose();
    _additionalInfoController.dispose();
    _tipController.dispose();
    _messageController.dispose();
    super.dispose();
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (!_isInitialized) {
      _initEmissionPoints();
      _isInitialized = true;
    }
  }



  Future<void> _initEmissionPoints() async {
    final emissionPointProvider = Provider.of<EmissionPointProvider>(
        context, listen: false);

    try {
      final branchData = await _storageService.getBranch();

      if (branchData != null && branchData.containsKey('id') &&
          branchData['id'] != null) {
        int branchId;
        if (branchData['id'] is String) {
          branchId = int.tryParse(branchData['id']) ?? 1;
        } else {
          branchId = branchData['id'];
        }

        print("Obteniendo puntos de emisión para la sucursal ID: $branchId");
        await emissionPointProvider.loadEmissionPoints(branchId);
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'No se pudo determinar la sucursal actual. Usando sucursal predeterminada.',
              style: TextStyle(fontFamily: 'Montserrat'),
            ),
            backgroundColor: Colors.orange,
            duration: Duration(seconds: 3),
          ),
        );
      }
    } catch (e) {
      print("Error al obtener puntos de emisión: $e");

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Error al cargar puntos de emisión: $e',
            style: TextStyle(fontFamily: 'Montserrat'),
          ),
          backgroundColor: Colors.red,
          duration: Duration(seconds: 3),
        ),
      );
    }
  }

  Future<void> _loadArticles() async {
    if (_isLoadingArticles) return;

    setState(() {
      _isLoadingArticles = true;
    });

    try {
      // Obtener el ID de la empresa
      final enterpriseData = await _storageService.getEnterprise();

      if (enterpriseData != null && enterpriseData.containsKey('id')) {
        final int enterpriseId = enterpriseData['id'];

        // Usar el servicio de artículos para obtener los productos
        final articleService = ArticleService(ApiClient());
        _articles = await articleService.getStockArticlesByEnterprise(enterpriseId);

        print("Artículos cargados: ${_articles.length}");
      } else {
        throw Exception('No se pudo obtener el ID de la empresa');
      }
    } catch (e) {
      print("Error al cargar artículos: $e");
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Error al cargar los productos: $e',
            style: TextStyle(fontFamily: 'Montserrat'),
          ),
          backgroundColor: Colors.red,
        ),
      );
    } finally {
      setState(() {
        _isLoadingArticles = false;
      });
    }
  }

  void _addArticleToInvoice(Article article) {
    final existingIndex = _selectedArticles.indexWhere((item) =>
    item['id'] == article.id);

    if (existingIndex >= 0) {
      setState(() {
        _selectedArticles[existingIndex]['quantity'] += 1;
        _calculateTotal(existingIndex);
      });
    } else {
      setState(() {
        _selectedArticles.add({
          'id': article.id,
          'code': article.code,
          'name': article.name,
          'description': article.description,
          'price': article.priceUnit,
          'quantity': 1,
          'total': article.priceUnit,
          'article': article,
        });
      });
    }
  }

  void _removeArticle(int index) {
    setState(() {
      _selectedArticles.removeAt(index);
    });
  }

  void _updateQuantity(int index, int quantity) {
    if (quantity <= 0) return;

    setState(() {
      _selectedArticles[index]['quantity'] = quantity;
      _calculateTotal(index);
    });
  }

  void _calculateTotal(int index) {
    final double price = _selectedArticles[index]['price'];
    final int quantity = _selectedArticles[index]['quantity'];
    final double discount = _selectedArticles[index]['discount'] ?? 0;

    // Calcular subtotal (precio * cantidad)
    double subtotal = price * quantity;

    // Aplicar descuento
    double discountAmount = subtotal * (discount / 100);

    // Total final
    _selectedArticles[index]['total'] = subtotal - discountAmount;
  }

  double get _invoiceTotal {
    if (_selectedArticles.isEmpty) return 0;
    return _selectedArticles.fold(
        0, (sum, item) => sum + (item['total'] as double));
  }

  List<Article> get _filteredArticles {
    // Primero filtrar por el criterio de búsqueda
    List<Article> searchFiltered = _searchQuery.isEmpty
        ? _articles
        : _articles.where((article) {
      return article.name.toLowerCase().contains(_searchQuery.toLowerCase()) ||
          article.code.toLowerCase().contains(_searchQuery.toLowerCase());
    }).toList();

    // Luego filtrar por stock > 0
    return searchFiltered.where((article) => article.stock > 0).toList();
  }

  Widget _buildStepIndicator(int step, String label, bool isActive) {
    return Expanded(
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 24,
            height: 24,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: isActive ? Colors.purple : Colors.grey.shade400,
            ),
            child: Center(
              child: Text(
                "$step",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  color: Colors.white,
                  fontWeight: FontWeight.bold,
                  fontSize: 12,
                ),
              ),
            ),
          ),
          SizedBox(width: 4),
          Text(
            label,
            style: TextStyle(
              fontFamily: 'Montserrat',
              color: isActive ? Colors.purple.shade700 : Colors.grey.shade600,
              fontWeight: isActive ? FontWeight.bold : FontWeight.normal,
              fontSize: 12,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildStepSeparator() {
    return Container(
      width: 20,
      height: 1,
      color: Colors.grey.shade400,
    );
  }

  @override
  Widget build(BuildContext context) {
    final emissionPointProvider = Provider.of<EmissionPointProvider>(context);
    final isLoading = emissionPointProvider.isLoading;
    final hasError = emissionPointProvider.error != null;
    final hasEmissionPoints = emissionPointProvider.hasEmissionPoints;

    return Scaffold(
      backgroundColor: Colors.grey.shade100,
      appBar: AppBar(
        backgroundColor: Colors.purple,
        elevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios, color: Colors.white),
          onPressed: () => Navigator.pop(context),
        ),
        title: Text(
          "Emitir Factura",
          style: TextStyle(
            fontFamily: 'Montserrat',
            fontWeight: FontWeight.bold,
            color: Colors.white,
          ),
        ),
        actions: [
          IconButton(
            icon: Icon(Icons.refresh, color: Colors.white),
            onPressed: _initEmissionPoints,
          ),
        ],
      ),
      body: SafeArea(
        child: Column(
          children: [
            // Indicador de pasos
            Container(
              padding: EdgeInsets.symmetric(vertical: 8, horizontal: 16),
              color: Colors.grey.shade200,
              child: Row(
                children: [
                  _buildStepIndicator(1, "Datos", _currentStep >= 0),
                  _buildStepSeparator(),
                  _buildStepIndicator(2, "Productos", _currentStep >= 1),
                  _buildStepSeparator(),
                  _buildStepIndicator(3, "Confirmar", _currentStep >= 2),
                ],
              ),
            ),

            // Contenido principal
            Expanded(
              child: isLoading
                  ? Center(child: CircularProgressIndicator(color: Colors.purple))
                  : hasError
                  ? _buildErrorView(emissionPointProvider.error!)
                  : !hasEmissionPoints
                  ? _buildNoEmissionPointsView()
                  : _currentStep == 0
                  ? _buildStep1Content()
                  : _currentStep == 1
                  ? _buildStep2Content()
                  : _buildStep3Content(),
            ),

            // Botones de navegación
            Container(
              padding: EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: Colors.white,
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.05),
                    blurRadius: 10,
                    offset: Offset(0, -2),
                  ),
                ],
              ),
              child: Row(
                children: [
                  if (_currentStep > 0)
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () {
                          setState(() => _currentStep -= 1);
                        },
                        style: OutlinedButton.styleFrom(
                          padding: EdgeInsets.symmetric(vertical: 12),
                          foregroundColor: Colors.purple.shade700,
                          side: BorderSide(color: Colors.purple.shade200),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(8),
                          ),
                        ),
                        child: Text(
                          "Anterior",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ),
                  if (_currentStep > 0) SizedBox(width: 16),
                  Expanded(
                    child: ElevatedButton(
                      // Controlar la habilitación del botón en el primer paso
                      onPressed: _isNextButtonEnabled() ? () {
                        if (_currentStep < 2) {
                          if (_currentStep == 0) {
                            if (_formKey.currentState!.validate()) {
                              // Si estamos pasando al paso de artículos, cargar los artículos
                              if (_currentStep == 0) {
                                _loadArticles();
                              }
                              setState(() => _currentStep += 1);
                            }
                          } else {
                            // Validar que haya al menos un artículo seleccionado
                            if (_selectedArticles.isEmpty) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                SnackBar(
                                  content: Text(
                                    'Debe seleccionar al menos un producto',
                                    style: TextStyle(fontFamily: 'Montserrat'),
                                  ),
                                  backgroundColor: Colors.red,
                                ),
                              );
                              return;
                            }
                            setState(() => _currentStep += 1);
                          }
                        } else {
                          // Aquí iría la lógica para emitir la factura
                          _emitInvoice();
                        }
                      } : null, // El botón se deshabilita si _isNextButtonEnabled() devuelve false
                      child: Text(
                        _currentStep < 2 ? "Siguiente" : "Finalizar",
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      style: ElevatedButton.styleFrom(
                        padding: EdgeInsets.symmetric(vertical: 12),
                        backgroundColor: _currentStep < 2 ? Colors.purple : Colors.green.shade600,
                        foregroundColor: Colors.white,
                        // Color de fondo cuando está deshabilitado
                        disabledBackgroundColor: Colors.grey.shade300,
                        disabledForegroundColor: Colors.grey.shade600,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(8),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  bool _isNextButtonEnabled() {
    final emissionPointProvider = Provider.of<EmissionPointProvider>(context, listen: false);

    // Si no estamos en el primer paso, el botón siempre está habilitado
    if (_currentStep > 0) return true;

    // Validaciones específicas para el primer paso
    if (_currentStep == 0) {
      // Verificar que se haya seleccionado un punto de emisión
      final selectedEmissionPoint = emissionPointProvider.selectedEmissionPoint;
      if (selectedEmissionPoint == null) return false;

      // Verificar que haya información de secuencia disponible
      bool hasSequenceInfo = false;

      // Verificar si hay secuencia en el punto de emisión seleccionado
      if (selectedEmissionPoint['sequences'] != null &&
          (selectedEmissionPoint['sequences'] as List).isNotEmpty) {
        hasSequenceInfo = true;
      }

      // O verificar si tenemos la última secuencia emitida
      if (emissionPointProvider.lastInvoiceSequence != null) {
        hasSequenceInfo = true;
      }

      return hasSequenceInfo;
    }

    return true; // Por defecto habilitado
  }

  Widget _buildStepper() {
    return Stepper(
      type: StepperType.horizontal,
      physics: ScrollPhysics(),
      currentStep: _currentStep,
      onStepTapped: (step) => setState(() => _currentStep = step),
      controlsBuilder: (context, details) {
        return Padding(
          padding: const EdgeInsets.only(top: 20),
          child: Row(
            children: [
              if (_currentStep > 0)
                Expanded(
                  child: OutlinedButton(
                    onPressed: details.onStepCancel,
                    child: Text(
                      "Anterior",
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    style: OutlinedButton.styleFrom(
                      padding: EdgeInsets.symmetric(vertical: 12),
                      foregroundColor: AppColors.textMedium,
                      side: BorderSide(color: AppColors.cream),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                    ),
                  ),
                ),
              if (_currentStep > 0) SizedBox(width: 12),
              Expanded(
                child: ElevatedButton(
                  onPressed: details.onStepContinue,
                  child: Text(
                    _currentStep < 2 ? "Siguiente" : "Finalizar",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  style: ElevatedButton.styleFrom(
                    padding: EdgeInsets.symmetric(vertical: 12),
                    backgroundColor: _currentStep < 2 ? AppColors.gold : Colors
                        .green.shade600,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(8),
                    ),
                  ),
                ),
              ),
            ],
          ),
        );
      },
      onStepContinue: () {
        if (_currentStep == 0) {
          // Validar datos del primer paso
          if (_formKey.currentState!.validate()) {
            // Si estamos pasando al paso de artículos, cargar los artículos
            if (_currentStep == 0) {
              _loadArticles();
            }
            setState(() => _currentStep += 1);
          }
        } else if (_currentStep == 1) {
          // Validar que haya al menos un artículo seleccionado
          if (_selectedArticles.isEmpty) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text(
                  'Debe seleccionar al menos un producto',
                  style: TextStyle(fontFamily: 'Montserrat'),
                ),
                backgroundColor: Colors.red,
              ),
            );
            return;
          }
          setState(() => _currentStep += 1);
        } else if (_currentStep == 2) {
          // Aquí iría la lógica para emitir la factura
          _emitInvoice();
        }
      },
      onStepCancel: () {
        if (_currentStep > 0) {
          setState(() => _currentStep -= 1);
        }
      },
      steps: [
        Step(
          title: Text(
            "Datos",
            style: TextStyle(
              fontFamily: 'Montserrat',
              fontSize: 14,
              fontWeight: FontWeight.bold,
            ),
          ),
          content: _buildStep1Content(),
          isActive: _currentStep >= 0,
          state: _currentStep > 0 ? StepState.complete : StepState.indexed,
        ),
        Step(
          title: Text(
            "Productos",
            style: TextStyle(
              fontFamily: 'Montserrat',
              fontSize: 14,
              fontWeight: FontWeight.bold,
            ),
          ),
          content: _buildStep2Content(),
          isActive: _currentStep >= 1,
          state: _currentStep > 1 ? StepState.complete : StepState.indexed,
        ),
        Step(
          title: Text(
            "Confirmar",
            style: TextStyle(
              fontFamily: 'Montserrat',
              fontSize: 14,
              fontWeight: FontWeight.bold,
            ),
          ),
          content: _buildStep3Content(),
          isActive: _currentStep >= 2,
          state: _currentStep == 2 ? StepState.indexed : StepState.disabled,
        ),
      ],
    );
  }

  Widget _buildCompanyInfo() {
    return FutureBuilder<Map<String, dynamic>?>(
      future: _storageService.getEnterprise(),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Container(
            height: 80,
            alignment: Alignment.center,
            child: CircularProgressIndicator(color: Colors.purple.shade700, strokeWidth: 2),
          );
        }

        if (snapshot.hasError || !snapshot.hasData || snapshot.data == null) {
          return Container(
            padding: EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Colors.red.shade50,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: Colors.red.shade200),
            ),
            child: Row(
              children: [
                Icon(Icons.error_outline, color: Colors.red.shade700),
                SizedBox(width: 8),
                Expanded(
                  child: Text(
                    "No se pudo cargar la información de la empresa",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontSize: 13,
                      color: Colors.red.shade700,
                    ),
                  ),
                ),
              ],
            ),
          );
        }

        final enterpriseData = snapshot.data!;
        final companyName = enterpriseData['companyName'] ?? "";
        final ruc = enterpriseData['ruc'] ?? "";

        return Container(
          padding: EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(8),
            border: Border.all(color: Colors.grey.shade200),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                "Información de la Empresa",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: Colors.purple.shade700,
                ),
              ),
              SizedBox(height: 8),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SizedBox(
                    width: 80, // Ancho fijo para las etiquetas
                    child: Text(
                      "Empresa:",
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 13,
                        color: Colors.grey.shade700,
                      ),
                    ),
                  ),
                  Expanded(
                    child: Text(
                      companyName,
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: Colors.black,
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
              SizedBox(height: 4),
              Row(
                children: [
                  SizedBox(
                    width: 80, // Ancho fijo para las etiquetas
                    child: Text(
                      "RUC:",
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 13,
                        color: Colors.grey.shade700,
                      ),
                    ),
                  ),
                  Expanded(
                    child: Text(
                      ruc,
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: Colors.black,
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }

// Ahora, implementemos una versión mejorada del contenedor de secuencia
  Widget _buildSequenceInfo() {
    final emissionPointProvider = Provider.of<EmissionPointProvider>(context);
    final selectedEmissionPoint = emissionPointProvider.selectedEmissionPoint;

    // Calcular la próxima secuencia
    String nextSequence = '';
    if (selectedEmissionPoint != null && emissionPointProvider.lastInvoiceSequence != null) {
      // Obtener la última secuencia como string
      String lastSequence = emissionPointProvider.lastInvoiceSequence!;

      // Convertir a entero, incrementar y formatear de vuelta a string con ceros a la izquierda
      int sequenceNumber = int.tryParse(lastSequence) ?? 0;
      sequenceNumber++;
      nextSequence = sequenceNumber.toString().padLeft(lastSequence.length, '0');
    } else if (selectedEmissionPoint != null &&
        selectedEmissionPoint['sequences'] != null &&
        (selectedEmissionPoint['sequences'] as List).isNotEmpty) {
      nextSequence = (selectedEmissionPoint['sequences'] as List).first['code'];
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Dropdown y secuencia actual
        Container(
          decoration: BoxDecoration(
            border: Border.all(color: Colors.grey.shade300),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              DropdownButtonFormField<int>(
                decoration: InputDecoration(
                  labelText: "Seleccione punto de emisión",
                  border: InputBorder.none,
                  contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                ),
                isExpanded: true,
                value: selectedEmissionPoint != null ? selectedEmissionPoint['idEmissionPoint'] : null,
                items: emissionPointProvider.emissionPoints.map((emissionPoint) {
                  return DropdownMenuItem<int>(
                    value: emissionPoint['idEmissionPoint'],
                    child: Text(
                      "${emissionPoint['code']} - ${emissionPoint['details']}",
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 14,
                      ),
                      overflow: TextOverflow.ellipsis,
                      maxLines: 1,
                    ),
                  );
                }).toList(),
                validator: (value) {
                  if (value == null) {
                    return 'Por favor seleccione un punto de emisión';
                  }
                  return null;
                },
                onChanged: (value) {
                  if (value != null) {
                    final selected = emissionPointProvider.emissionPoints.firstWhere(
                          (emissionPoint) => emissionPoint['idEmissionPoint'] == value,
                    );
                    emissionPointProvider.selectEmissionPoint(selected);

                    // Añadir setState para actualizar el estado del botón
                    setState(() {});
                  }
                },
              ),
              if (selectedEmissionPoint != null &&
                  selectedEmissionPoint['sequences'] != null &&
                  (selectedEmissionPoint['sequences'] as List).isNotEmpty)
                Padding(
                  padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  child: Row(
                    children: [
                      Icon(
                        Icons.info_outline,
                        size: 14,
                        color: Colors.purple.shade700,
                      ),
                      SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          "Secuencia actual: ${(selectedEmissionPoint['sequences'] as List).first['code']}",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 12,
                            color: Colors.grey.shade700,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
            ],
          ),
        ),
        SizedBox(height: 16),

        // Información de última secuencia
        if (selectedEmissionPoint != null && emissionPointProvider.lastInvoiceSequence != null)
          Container(
            padding: EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Colors.purple.shade50,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: Colors.purple.shade200),
            ),
            child: Row(
              children: [
                Icon(
                  Icons.receipt_long,
                  color: Colors.purple.shade700,
                  size: 22,
                ),
                SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        "Información de Secuencia",
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 13,
                          fontWeight: FontWeight.bold,
                          color: Colors.purple.shade700,
                        ),
                      ),
                      SizedBox(height: 3),
                      Text(
                        "Última secuencia emitida: ${emissionPointProvider.lastInvoiceSequence}",
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 12,
                          color: Colors.purple.shade900,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        SizedBox(height: 16),

        // Campo de secuencia a utilizar
        if (selectedEmissionPoint != null && nextSequence.isNotEmpty)
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                "Secuencia a utilizar",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: Colors.grey.shade700,
                ),
              ),
              SizedBox(height: 6),
              Container(
                decoration: BoxDecoration(
                  border: Border.all(color: Colors.purple.shade300),
                  borderRadius: BorderRadius.circular(8),
                  color: Colors.grey.shade100,
                ),
                child: Row(
                  children: [
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 12, vertical: 12),
                      decoration: BoxDecoration(
                        color: Colors.purple.shade100,
                        borderRadius: BorderRadius.only(
                          topLeft: Radius.circular(7),
                          bottomLeft: Radius.circular(7),
                        ),
                      ),
                      child: Icon(
                        Icons.pin,
                        size: 20,
                        color: Colors.purple.shade700,
                      ),
                    ),
                    SizedBox(width: 12),
                    Text(
                      nextSequence,
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: Colors.purple.shade700,
                      ),
                    ),
                    Spacer(),
                    Padding(
                      padding: EdgeInsets.only(right: 12),
                      child: Icon(
                        Icons.lock_outline,
                        size: 18,
                        color: Colors.grey.shade500,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),

      ],
    );
  }


  // Añadir estos métodos a la clase _CreateInvoiceScreenState

// Método para buscar cliente por DNI/RUC
  Future<void> _searchClientByDni() async {
    // Verificar que no estamos en modo consumidor final
    if (_isFinalConsumer) {
      setState(() {
        _isFinalConsumer = false;
        _clientIdController.text = ''; // Limpiar el campo para que el usuario ingrese un DNI
        _clientNameController.text = '';
        _clientAddressController.text = '';
        _clientEmailController.text = '';
      });
      return;
    }

    final dni = _clientIdController.text.trim();

    if (dni.isEmpty) {
      setState(() {
        _clientErrorMessage = 'Ingrese un número de identificación';
      });
      return;
    }

    setState(() {
      _isSearchingClient = true;
      _clientErrorMessage = '';
    });

    try {
      final apiClient = ApiClient();
      final response = await apiClient.get('/Client/dni/$dni');

      setState(() {
        _isSearchingClient = false;
      });

      if (response.statusCode == 200 && response.data != null) {
        // Cliente encontrado, actualizar campos
        final clientData = Map<String, dynamic>.from(response.data);

        setState(() {
          _clientIdController.text = clientData['dni'] ?? '';
          _clientNameController.text = clientData['razon_social'] ?? '';
          _clientAddressController.text = clientData['address'] ?? '';
          _clientEmailController.text = clientData['email'] ?? '';
          _selectedClientId = clientData['id_client']; // Guardar ID del cliente
          _clientErrorMessage = ''; // Limpiar mensajes de error previos
        });

        // Mostrar mensaje de éxito
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'Cliente encontrado',
              style: TextStyle(fontFamily: 'Montserrat'),
            ),
            backgroundColor: Colors.green,
            duration: Duration(seconds: 2),
          ),
        );
      } else {
        // Cliente no encontrado o error en la respuesta
        String errorMessage = 'Cliente no encontrado';

        // Intentar extraer mensaje de error específico
        if (response.data is Map && response.data['Message'] != null) {
          errorMessage = response.data['Message'];
        }

        setState(() {
          _clientErrorMessage = errorMessage;
        });

        // Mostrar diálogo para crear nuevo cliente
        _showCreateClientDialog();
      }
    } catch (e) {
      print('Error al buscar cliente: $e');
      setState(() {
        _isSearchingClient = false;
        _clientErrorMessage = 'Error al buscar cliente';
      });

      // Mostrar diálogo para crear nuevo cliente
      _showCreateClientDialog();
    }
  }

// Diálogo para sugerir crear un nuevo cliente
  void _showCreateClientDialog() {
    showDialog(
      context: context,
      builder: (BuildContext context) {
        return AlertDialog(
          title: Text(
            'Cliente no encontrado',
            style: TextStyle(
              fontFamily: 'Montserrat',
              fontWeight: FontWeight.bold,
            ),
          ),
          content: Text(
            'El cliente con el número de identificación ingresado no existe. ¿Desea registrar un nuevo cliente?',
            style: TextStyle(
              fontFamily: 'Montserrat',
            ),
          ),
          actions: [
            TextButton(
              child: Text(
                'Cancelar',
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  color: Colors.grey,
                ),
              ),
              onPressed: () {
                Navigator.of(context).pop();
              },
            ),
            ElevatedButton(
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.purple,
              ),
              child: Text(
                'Crear Cliente',
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  color: Colors.white,
                ),
              ),
              onPressed: () {
                Navigator.of(context).pop();
                _navigateToCreateClient();
              },
            ),
          ],
        );
      },
    );
  }

// Método para navegar a la pantalla de creación de cliente
  Future<void> _navigateToCreateClient() async {
    // Guardamos el DNI actual para usarlo en la pantalla de creación
    final dni = _clientIdController.text.trim();

    final result = await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => ClientFormScreen(
          initialDni: dni, // Pasamos el DNI como parámetro
        ),
      ),
    );

    // Si se creó el cliente correctamente, volvemos a buscarlo
    if (result == true) {
      _searchClientByDni();
    }
  }

  Widget _buildStep1Content() {
    return SingleChildScrollView(
      padding: EdgeInsets.all(16),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Sección de punto de emisión
            Text(
              "Punto de Emisión",
              style: TextStyle(
                fontFamily: 'Montserrat',
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: Colors.black,
              ),
            ),
            SizedBox(height: 10),
            _buildSequenceInfo(),
            if (!_isNextButtonEnabled() && _currentStep == 0)
              Container(
                margin: EdgeInsets.only(top: 16),
                padding: EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.orange.shade50,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.orange.shade200),
                ),
                child: Row(
                  children: [
                    Icon(
                      Icons.info_outline,
                      color: Colors.orange.shade700,
                      size: 20,
                    ),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        "Por favor seleccione un punto de emisión para continuar.",
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 12,
                          color: Colors.orange.shade900,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            SizedBox(height: 24),

            // Información de la empresa
            _buildCompanyInfo(),
            SizedBox(height: 24),

            // Datos del cliente
            Text(
              "Datos del Cliente",
              style: TextStyle(
                fontFamily: 'Montserrat',
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: Colors.black,
              ),
            ),
            SizedBox(height: 10),

            // Selector de tipo de cliente
            Row(
              children: [
                Expanded(
                  child: InkWell(
                    onTap: () {
                      setState(() {
                        _isFinalConsumer = true;
                        _clientIdController.text = '9999999999999';
                        _clientNameController.text = 'CONSUMIDOR FINAL';
                        _clientAddressController.text = 'S/N';
                        _clientEmailController.text = '';
                      });
                    },
                    child: Container(
                      padding: EdgeInsets.symmetric(vertical: 12),
                      decoration: BoxDecoration(
                        color: _isFinalConsumer ? Colors.purple.withOpacity(0.1) : Colors.transparent,
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(
                          color: _isFinalConsumer ? Colors.purple : Colors.grey.shade300,
                          width: 1,
                        ),
                      ),
                      child: Column(
                        children: [
                          Icon(
                            Icons.person_outline,
                            color: _isFinalConsumer ? Colors.purple : Colors.grey.shade600,
                            size: 24,
                          ),
                          SizedBox(height: 8),
                          Text(
                            "Consumidor Final",
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              color: _isFinalConsumer ? Colors.purple : Colors.grey.shade600,
                              fontWeight: _isFinalConsumer ? FontWeight.bold : FontWeight.normal,
                            ),
                            textAlign: TextAlign.center,
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
                SizedBox(width: 12),
                Expanded(
                  child: InkWell(
                    onTap: () {
                      setState(() {
                        _isFinalConsumer = false;
                        _clientIdController.text = '';
                        _clientNameController.text = '';
                        _clientAddressController.text = '';
                        _clientEmailController.text = '';
                      });
                    },
                    child: Container(
                      padding: EdgeInsets.symmetric(vertical: 12),
                      decoration: BoxDecoration(
                        color: !_isFinalConsumer ? Colors.purple.withOpacity(0.1) : Colors.transparent,
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(
                          color: !_isFinalConsumer ? Colors.purple : Colors.grey.shade300,
                          width: 1,
                        ),
                      ),
                      child: Column(
                        children: [
                          Icon(
                            Icons.business,
                            color: !_isFinalConsumer ? Colors.purple : Colors.grey.shade600,
                            size: 24,
                          ),
                          SizedBox(height: 8),
                          Text(
                            "Con Datos",
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              color: !_isFinalConsumer ? Colors.purple : Colors.grey.shade600,
                              fontWeight: !_isFinalConsumer ? FontWeight.bold : FontWeight.normal,
                            ),
                            textAlign: TextAlign.center,
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ),

            SizedBox(height: 16),

            // Campos de cliente (solo visibles si no es consumidor final)
            if (!_isFinalConsumer) ...[
              Container(
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(
                    color: _clientErrorMessage.isNotEmpty ? Colors.red : Colors.grey.shade300,
                  ),
                ),
                child: Row(
                  children: [
                    Expanded(
                      child: TextFormField(
                        controller: _clientIdController,
                        decoration: InputDecoration(
                          labelText: "RUC / Cédula",
                          prefixIcon: Icon(Icons.badge_outlined),
                          border: InputBorder.none,
                          contentPadding: EdgeInsets.symmetric(horizontal: 16),
                        ),
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Por favor ingrese el RUC o cédula';
                          }
                          if (value.length != 10 && value.length != 13) {
                            return 'Ingrese un RUC o cédula válido';
                          }
                          return null;
                        },
                      ),
                    ),
                    Container(
                      height: 56,
                      decoration: BoxDecoration(
                        border: Border(
                          left: BorderSide(color: Colors.grey.shade300),
                        ),
                      ),
                      child: IconButton(
                        icon: _isSearchingClient
                            ? SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.purple,
                          ),
                        )
                            : Icon(Icons.search, color: Colors.purple),
                        onPressed: _isSearchingClient ? null : _searchClientByDni,
                        tooltip: 'Buscar cliente',
                      ),
                    ),
                  ],
                ),
              ),

// Añadir esto después del campo de identificación para mostrar mensajes de error
              if (_clientErrorMessage.isNotEmpty)
                Padding(
                  padding: const EdgeInsets.only(left: 12, top: 4),
                  child: Row(
                    children: [
                      Icon(
                        Icons.error_outline,
                        color: Colors.red,
                        size: 14,
                      ),
                      SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          _clientErrorMessage,
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            color: Colors.red,
                            fontSize: 12,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              SizedBox(height: 16),
              TextFormField(
                controller: _clientNameController,
                decoration: InputDecoration(
                  labelText: "Nombre / Razón Social",
                  prefixIcon: Icon(Icons.business_outlined),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return 'Por favor ingrese el nombre o razón social';
                  }
                  return null;
                },
              ),
              SizedBox(height: 16),
              TextFormField(
                controller: _clientAddressController,
                decoration: InputDecoration(
                  labelText: "Dirección",
                  prefixIcon: Icon(Icons.location_on_outlined),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return 'Por favor ingrese la dirección';
                  }
                  return null;
                },
              ),
              SizedBox(height: 16),
              TextFormField(
                controller: _clientEmailController,
                decoration: InputDecoration(
                  labelText: "Email",
                  prefixIcon: Icon(Icons.email_outlined),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
                keyboardType: TextInputType.emailAddress,
                validator: (value) {
                  if (value != null && value.isNotEmpty) {
                    // Validar formato de email
                    final emailRegExp = RegExp(r'^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$');
                    if (!emailRegExp.hasMatch(value)) {
                      return 'Por favor ingrese un email válido';
                    }
                  }
                  return null;
                },
              ),
            ] else ...[
              // Si es consumidor final, mostrar información resumida
              Container(
                padding: EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.grey.shade100,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Row(
                  children: [
                    Icon(
                      Icons.info_outline,
                      color: Colors.grey.shade700,
                      size: 20,
                    ),
                    SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        "Se utilizarán los datos predeterminados para Consumidor Final",
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 14,
                          color: Colors.grey.shade700,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
  Widget _buildStep2Content() {
    return FutureBuilder<double>(
      // Llamada al endpoint que obtiene la tasa de IVA
      future: ArticleService(ApiClient()).getTaxRateByCode(4),
      builder: (context, snapshot) {
        // Valor predeterminado o valor obtenido del endpoint
        final double ivaRate = snapshot.hasData ? snapshot.data! : 15.0;

        return SingleChildScrollView(
          padding: EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Buscador de productos
              Text(
                "Buscar Productos",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: Colors.black,
                ),
              ),
              SizedBox(height: 10),
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.grey.shade200),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    TextField(
                      decoration: InputDecoration(
                        hintText: "Buscar por nombre o código...",
                        prefixIcon: Icon(Icons.search, color: Colors.purple),
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(8),
                          borderSide: BorderSide(color: Colors.grey.shade300),
                        ),
                        contentPadding: EdgeInsets.symmetric(vertical: 0, horizontal: 16),
                      ),
                      onChanged: (value) {
                        setState(() {
                          _searchQuery = value;
                        });
                      },
                    ),
                    SizedBox(height: 16),

                    // Mostrar indicador de carga mientras se obtiene la tasa de IVA
                    if (snapshot.connectionState == ConnectionState.waiting)
                      Center(
                        child: Padding(
                          padding: EdgeInsets.symmetric(vertical: 20),
                          child: CircularProgressIndicator(color: Colors.purple),
                        ),
                      )
                    else if (_isLoadingArticles)
                      Container(
                        height: 120,
                        alignment: Alignment.center,
                        child: CircularProgressIndicator(color: Colors.purple),
                      )
                    else if (_filteredArticles.isEmpty)
                        Container(
                          height: 120,
                          alignment: Alignment.center,
                          child: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Icon(
                                Icons.search_off,
                                size: 32,
                                color: Colors.grey.shade400,
                              ),
                              SizedBox(height: 8),
                              Text(
                                "No se encontraron productos",
                                style: TextStyle(
                                  fontFamily: 'Montserrat',
                                  fontSize: 14,
                                  color: Colors.grey.shade600,
                                ),
                              ),
                            ],
                          ),
                        )
                      else
                        Container(
                          height: 200,
                          decoration: BoxDecoration(
                            border: Border.all(color: Colors.grey.shade200),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child:
                          ListView.separated(
                            padding: EdgeInsets.all(8),
                            itemCount: _filteredArticles.length,
                            separatorBuilder: (context, index) => Divider(height: 1, color: Colors.grey.shade200),
                            itemBuilder: (context, index) {
                              final article = _filteredArticles[index];
                              final bool hasStock = article.stock > 0;

                              return ListTile(
                                contentPadding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                title: Text(
                                  article.name,
                                  style: TextStyle(
                                    fontFamily: 'Montserrat',
                                    fontWeight: FontWeight.w600,
                                    // Agregar color gris si no hay stock
                                    color: hasStock ? Colors.black : Colors.grey.shade400,
                                  ),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                ),
                                subtitle: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      "Código: ${article.code} - Precio: \$${article.priceUnit.toStringAsFixed(2)}",
                                      style: TextStyle(
                                        fontFamily: 'Montserrat',
                                        fontSize: 12,
                                        color: hasStock ? Colors.grey.shade700 : Colors.grey.shade400,
                                      ),
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                    // Agregar indicador de stock
                                    Text(
                                      hasStock
                                          ? "Stock: ${article.stock}"
                                          : "Sin stock disponible",
                                      style: TextStyle(
                                        fontFamily: 'Montserrat',
                                        fontSize: 12,
                                        fontWeight: hasStock ? FontWeight.normal : FontWeight.bold,
                                        color: hasStock ? Colors.green.shade700 : Colors.red.shade400,
                                      ),
                                    ),
                                  ],
                                ),
                                trailing: IconButton(
                                  icon: Container(
                                    padding: EdgeInsets.all(4),
                                    decoration: BoxDecoration(
                                      color: hasStock ? Colors.purple.withOpacity(0.1) : Colors.grey.shade200,
                                      shape: BoxShape.circle,
                                    ),
                                    child: Icon(
                                        Icons.add,
                                        color: hasStock ? Colors.purple : Colors.grey.shade400,
                                        size: 20
                                    ),
                                  ),
                                  onPressed: hasStock
                                      ? () => _addArticleToInvoice(article)
                                      : () {
                                    // Mostrar mensaje de error cuando se intenta agregar un producto sin stock
                                    ScaffoldMessenger.of(context).showSnackBar(
                                      SnackBar(
                                        content: Text(
                                          'El producto "${article.name}" no tiene stock disponible',
                                          style: TextStyle(fontFamily: 'Montserrat'),
                                        ),
                                        backgroundColor: Colors.red,
                                        duration: Duration(seconds: 2),
                                      ),
                                    );
                                  },
                                ),
                              );
                            },
                          ),
                        ),
                  ],
                ),
              ),

              SizedBox(height: 24),

              // Productos seleccionados
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    "Productos Seleccionados",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: Colors.black,
                    ),
                  ),
                  if (_selectedArticles.isNotEmpty)
                    TextButton.icon(
                      onPressed: () {
                        showDialog(
                          context: context,
                          builder: (context) => AlertDialog(
                            title: Text(
                              "Confirmar",
                              style: TextStyle(
                                fontFamily: 'Montserrat',
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            content: Text(
                              "¿Desea eliminar todos los productos seleccionados?",
                              style: TextStyle(fontFamily: 'Montserrat'),
                            ),
                            actions: [
                              TextButton(
                                onPressed: () => Navigator.pop(context),
                                child: Text(
                                  "Cancelar",
                                  style: TextStyle(fontFamily: 'Montserrat', color: Colors.grey.shade700),
                                ),
                              ),
                              ElevatedButton(
                                onPressed: () {
                                  setState(() {
                                    _selectedArticles.clear();
                                  });
                                  Navigator.pop(context);
                                },
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: Colors.red,
                                  foregroundColor: Colors.white,
                                ),
                                child: Text(
                                  "Eliminar",
                                  style: TextStyle(fontFamily: 'Montserrat'),
                                ),
                              ),
                            ],
                          ),
                        );
                      },
                      icon: Icon(Icons.delete_sweep, color: Colors.red.shade400, size: 16),
                      label: Text(
                        "Limpiar",
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          color: Colors.red.shade400,
                          fontSize: 12,
                        ),
                      ),
                      style: TextButton.styleFrom(
                        padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                      ),
                    ),
                ],
              ),
              SizedBox(height: 10),
              Container(
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.grey.shade200),
                ),
                child: _selectedArticles.isEmpty
                    ? Container(
                  height: 100,
                  alignment: Alignment.center,
                  padding: EdgeInsets.all(16),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(
                        Icons.shopping_cart_outlined,
                        size: 32,
                        color: Colors.grey.shade400,
                      ),
                      SizedBox(height: 8),
                      Text(
                        "No hay productos agregados",
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 14,
                          color: Colors.grey.shade600,
                          fontStyle: FontStyle.italic,
                        ),
                      ),
                    ],
                  ),
                )
                    : Column(
                  children: [
                    // Encabezados
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      decoration: BoxDecoration(
                        color: Colors.grey.shade100,
                        borderRadius: BorderRadius.only(
                          topLeft: Radius.circular(8),
                          topRight: Radius.circular(8),
                        ),
                      ),
                      child: Row(
                        children: [
                          Expanded(
                            flex: 4,
                            child: Text(
                              "Producto",
                              style: TextStyle(
                                fontFamily: 'Montserrat',
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                                color: Colors.grey.shade700,
                              ),
                            ),
                          ),
                          Expanded(
                            flex: 1,
                            child: Text(
                              "Cant.",
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                fontFamily: 'Montserrat',
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                                color: Colors.grey.shade700,
                              ),
                            ),
                          ),
                          Expanded(
                            flex: 2,
                            child: Text(
                              "Total",
                              textAlign: TextAlign.end,
                              style: TextStyle(
                                fontFamily: 'Montserrat',
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                                color: Colors.grey.shade700,
                              ),
                            ),
                          ),
                          SizedBox(width: 12), // Espacio para el botón de eliminar
                        ],
                      ),
                    ),
                    // Lista de productos
                    Container(
                      constraints: BoxConstraints(maxHeight: 300),
                      child: ListView.separated(
                        shrinkWrap: true,
                        itemCount: _selectedArticles.length,
                        separatorBuilder: (context, index) => Divider(height: 1, color: Colors.grey.shade200),
                        itemBuilder: (context, index) {
                          final item = _selectedArticles[index];
                          return InkWell(
                            onTap: () {
                              // Mostrar detalles o editar
                              _showArticleDetailDialog(index, ivaRate);
                            },
                            child: Padding(
                              padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                              child: Row(
                                children: [
                                  Expanded(
                                    flex: 4,
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        Text(
                                          item['name'],
                                          style: TextStyle(
                                            fontFamily: 'Montserrat',
                                            fontWeight: FontWeight.w600,
                                          ),
                                          maxLines: 1,
                                          overflow: TextOverflow.ellipsis,
                                        ),
                                        Text(
                                          "Precio: \$${item['price'].toStringAsFixed(2)}",
                                          style: TextStyle(
                                            fontFamily: 'Montserrat',
                                            fontSize: 12,
                                            color: Colors.grey.shade600,
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                  Expanded(
                                    flex: 2,
                                    child: Container(
                                      alignment: Alignment.center,
                                      child: Row(
                                        mainAxisSize: MainAxisSize.min,
                                        children: [
                                          InkWell(
                                            onTap: () => _updateQuantity(index, item['quantity'] - 1),
                                            child: Container(
                                              padding: EdgeInsets.all(2),
                                              decoration: BoxDecoration(
                                                color: Colors.grey.shade200,
                                                borderRadius: BorderRadius.circular(4),
                                              ),
                                              child: Icon(Icons.remove, size: 10),
                                            ),
                                          ),
                                          Container(
                                            width: 20,
                                            alignment: Alignment.center,
                                            padding: EdgeInsets.symmetric(horizontal: 4),
                                            child: Text(
                                              "${item['quantity']}",
                                              style: TextStyle(
                                                fontFamily: 'Montserrat',
                                                fontWeight: FontWeight.bold,
                                                fontSize: 12,
                                              ),
                                            ),
                                          ),
                                          InkWell(
                                            onTap: () => _updateQuantity(index, item['quantity'] + 1),
                                            child: Container(
                                              padding: EdgeInsets.all(2),
                                              decoration: BoxDecoration(
                                                color: Colors.purple.shade100,
                                                borderRadius: BorderRadius.circular(4),
                                              ),
                                              child: Icon(Icons.add, size: 10, color: Colors.purple),
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ),
                                  Expanded(
                                    flex: 2,
                                    child: Text(
                                      "\$${item['total'].toStringAsFixed(2)}",
                                      textAlign: TextAlign.end,
                                      style: TextStyle(
                                        fontFamily: 'Montserrat',
                                        fontWeight: FontWeight.bold,
                                      ),
                                    ),
                                  ),
                                  InkWell(
                                    onTap: () => _removeArticle(index),
                                    child: Padding(
                                      padding: EdgeInsets.symmetric(horizontal: 8),
                                      child: Icon(
                                        Icons.close,
                                        size: 16,
                                        color: Colors.red.shade400,
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          );
                        },
                      ),
                    ),
                    // Totales
                    Container(
                      padding: EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: Colors.grey.shade50,
                        borderRadius: BorderRadius.only(
                          bottomLeft: Radius.circular(8),
                          bottomRight: Radius.circular(8),
                        ),
                        border: Border(
                          top: BorderSide(color: Colors.grey.shade200),
                        ),
                      ),
                      child: Column(
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                "Subtotal:",
                                style: TextStyle(
                                  fontFamily: 'Montserrat',
                                  fontSize: 13,
                                ),
                              ),
                              Text(
                                "\$${(_invoiceTotal / (1 + (ivaRate / 100))).toStringAsFixed(2)}",
                                style: TextStyle(
                                  fontFamily: 'Montserrat',
                                  fontSize: 13,
                                ),
                              ),
                            ],
                          ),
                          SizedBox(height: 4),
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                "IVA (${ivaRate.toStringAsFixed(0)}%):",
                                style: TextStyle(
                                  fontFamily: 'Montserrat',
                                  fontSize: 13,
                                ),
                              ),
                              Text(
                                "\$${(_invoiceTotal - (_invoiceTotal / (1 + (ivaRate / 100)))).toStringAsFixed(2)}",
                                style: TextStyle(
                                  fontFamily: 'Montserrat',
                                  fontSize: 13,
                                ),
                              ),
                            ],
                          ),
                          SizedBox(height: 8),
                          Divider(height: 1, color: Colors.grey.shade300),
                          SizedBox(height: 8),
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                "TOTAL:",
                                style: TextStyle(
                                  fontFamily: 'Montserrat',
                                  fontSize: 14,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              Text(
                                "\$${_invoiceTotal.toStringAsFixed(2)}",
                                style: TextStyle(
                                  fontFamily: 'Montserrat',
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                  color: Colors.purple,
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }

// Método modificado para mostrar el diálogo de detalle/edición de artículo con IVA dinámico
  void _showArticleDetailDialog(int index, double ivaRate) {
    final item = _selectedArticles[index];
    final TextEditingController codeStubController = TextEditingController(text: item['codeStub'] ?? item['code']);
    final TextEditingController discountController = TextEditingController(text: (item['discount'] ?? 0).toString());

    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(
          "Detalles del Producto",
          style: TextStyle(
            fontFamily: 'Montserrat',
            fontWeight: FontWeight.bold,
            fontSize: 18,
          ),
        ),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                item['name'],
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontWeight: FontWeight.bold,
                  fontSize: 16,
                ),
              ),
              SizedBox(height: 4),
              Text(
                "Código: ${item['code']}",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 14,
                  color: Colors.grey.shade700,
                ),
              ),
              SizedBox(height: 4),
              Text(
                "Precio Unitario: \$${item['price'].toStringAsFixed(2)}",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 14,
                  color: Colors.grey.shade700,
                ),
              ),
              // Mostrar información del IVA
              SizedBox(height: 4),
              Text(
                "IVA: ${ivaRate.toStringAsFixed(0)}%",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 14,
                  color: Colors.grey.shade700,
                ),
              ),
              SizedBox(height: 16),
              TextField(
                controller: codeStubController,
                decoration: InputDecoration(
                  labelText: "Código Auxiliar",
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                  contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 12),
                ),
              ),
              SizedBox(height: 12),
              TextField(
                controller: discountController,
                decoration: InputDecoration(
                  labelText: "Descuento (%)",
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                  contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 12),
                ),
                keyboardType: TextInputType.numberWithOptions(decimal: true),
              ),
              SizedBox(height: 16),
              Text(
                "Cantidad",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontWeight: FontWeight.bold,
                  fontSize: 14,
                ),
              ),
              SizedBox(height: 8),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  ElevatedButton(
                    onPressed: () {
                      if (item['quantity'] > 1) {
                        setState(() {
                          item['quantity'] -= 1;
                          _calculateTotal(index);
                        });
                        Navigator.pop(context);
                        _showArticleDetailDialog(index, ivaRate);
                      }
                    },
                    child: Icon(Icons.remove),
                    style: ElevatedButton.styleFrom(
                      shape: CircleBorder(),
                      padding: EdgeInsets.all(8),
                      backgroundColor: Colors.grey.shade200,
                      foregroundColor: Colors.black,
                    ),
                  ),
                  Container(
                    padding: EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                    child: Text(
                      "${item['quantity']}",
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontWeight: FontWeight.bold,
                        fontSize: 18,
                      ),
                    ),
                  ),
                  ElevatedButton(
                    onPressed: () {
                      setState(() {
                        item['quantity'] += 1;
                        _calculateTotal(index);
                      });
                      Navigator.pop(context);
                      _showArticleDetailDialog(index, ivaRate);
                    },
                    child: Icon(Icons.add),
                    style: ElevatedButton.styleFrom(
                      shape: CircleBorder(),
                      padding: EdgeInsets.all(8),
                      backgroundColor: Colors.purple,
                      foregroundColor: Colors.white,
                    ),
                  ),
                ],
              ),

              // Mostrar subtotal e IVA
              SizedBox(height: 16),
              Container(
                padding: EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.grey.shade50,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.grey.shade200),
                ),
                child: Column(
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          "Subtotal:",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 13,
                          ),
                        ),
                        Text(
                          "\$${(item['total'] / (1 + (ivaRate / 100))).toStringAsFixed(2)}",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 13,
                          ),
                        ),
                      ],
                    ),
                    SizedBox(height: 4),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          "IVA (${ivaRate.toStringAsFixed(0)}%):",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 13,
                          ),
                        ),
                        Text(
                          "\$${(item['total'] - (item['total'] / (1 + (ivaRate / 100)))).toStringAsFixed(2)}",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 13,
                          ),
                        ),
                      ],
                    ),
                    SizedBox(height: 4),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          "Total:",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontWeight: FontWeight.bold,
                            fontSize: 14,
                          ),
                        ),
                        Text(
                          "\$${item['total'].toStringAsFixed(2)}",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontWeight: FontWeight.bold,
                            fontSize: 14,
                            color: Colors.purple,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: Text(
              "Cancelar",
              style: TextStyle(
                fontFamily: 'Montserrat',
                color: Colors.grey.shade700,
              ),
            ),
          ),
          ElevatedButton(
            onPressed: () {
              setState(() {
                // Actualizar valores del artículo
                item['codeStub'] = codeStubController.text;
                item['discount'] = double.tryParse(discountController.text) ?? 0;
                _calculateTotal(index);
              });
              Navigator.pop(context);
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.purple,
              foregroundColor: Colors.white,
            ),
            child: Text(
              "Guardar",
              style: TextStyle(
                fontFamily: 'Montserrat',
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
        ],
      ),
    );
  }
  Widget _buildStep3Content() {
    final emissionPointProvider = Provider.of<EmissionPointProvider>(context);
    final selectedEmissionPoint = emissionPointProvider.selectedEmissionPoint;

    // Calcular la próxima secuencia
    String nextSequence = '';
    String emissionPointCode = '';

    if (selectedEmissionPoint != null) {
      emissionPointCode = selectedEmissionPoint['code'] ?? '';

      if (emissionPointProvider.lastInvoiceSequence != null) {
        String lastSequence = emissionPointProvider.lastInvoiceSequence!;
        int sequenceNumber = int.tryParse(lastSequence) ?? 0;
        sequenceNumber++;
        nextSequence = sequenceNumber.toString().padLeft(lastSequence.length, '0');
      } else if (selectedEmissionPoint['sequences'] != null &&
          (selectedEmissionPoint['sequences'] as List).isNotEmpty) {
        nextSequence = (selectedEmissionPoint['sequences'] as List).first['code'];
      }
    }

    // Usar FutureBuilder para obtener la tasa de IVA desde el endpoint
    return FutureBuilder<double>(
      // Llamada al endpoint que obtiene la tasa de IVA
      future: ArticleService(ApiClient()).getTaxRateByCode(4),
      builder: (context, snapshot) {
        // Mostrar indicador de carga mientras se obtiene la tasa
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                CircularProgressIndicator(color: Colors.purple),
                SizedBox(height: 16),
                Text(
                  "Obteniendo información de impuestos...",
                  style: TextStyle(
                    fontFamily: 'Montserrat',
                    fontSize: 14,
                  ),
                ),
              ],
            ),
          );
        }

        // Manejar errores
        if (snapshot.hasError) {
          print("Error al obtener tasa de IVA: ${snapshot.error}");
        }

        // Obtener la tasa de IVA del resultado o usar valor predeterminado si hay error
        final double ivaRate = snapshot.hasData ? snapshot.data! : 15.0;

        // Calcular valores con la tasa de IVA obtenida
        final double subtotal = _invoiceTotal / (1 + (ivaRate / 100));
        final double ivaAmount = _invoiceTotal - subtotal;
        final double tipAmount = double.tryParse(_tipController.text) ?? 0.0;
        final double grandTotal = _invoiceTotal + tipAmount;

        // Contenido de la pantalla
        return SingleChildScrollView(
          padding: EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Información de la factura
              Text(
                "Resumen de Factura",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: Colors.black,
                ),
              ),
              SizedBox(height: 10),

              // Contenedor principal
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.grey.shade200),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Datos de la factura
                    _buildInfoRow("Punto de Emisión", emissionPointCode),
                    SizedBox(height: 4),
                    _buildInfoRow("Número de Factura", "$emissionPointCode-$nextSequence"),
                    SizedBox(height: 4),
                    _buildInfoRow("Fecha", "${DateTime.now().day}/${DateTime.now().month}/${DateTime.now().year}"),

                    Divider(height: 24, color: Colors.grey.shade300),

                    // Datos del cliente
                    Text(
                      "Datos del Cliente",
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: Colors.purple.shade700,
                      ),
                    ),
                    SizedBox(height: 8),
                    _buildInfoRow("Identificación", _clientIdController.text),
                    SizedBox(height: 4),
                    _buildInfoRow("Nombre / Razón Social", _clientNameController.text),
                    SizedBox(height: 4),
                    _buildInfoRow("Dirección", _clientAddressController.text),
                    if (_clientEmailController.text.isNotEmpty) ...[
                      SizedBox(height: 4),
                      _buildInfoRow("Email", _clientEmailController.text),
                    ],

                    Divider(height: 24, color: Colors.grey.shade300),

                    // Resumen de productos
                    Text(
                      "Productos",
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: Colors.purple.shade700,
                      ),
                    ),
                    SizedBox(height: 8),
                    _buildInfoRow("Cantidad de Productos", "${_selectedArticles.length} productos"),
                    SizedBox(height: 4),
                    _buildInfoRow("Total de Ítems", "${_selectedArticles.fold(0, (sum, item) => (sum + item['quantity']).toInt())} unidades"),
                    // Lista resumida de productos
                    SizedBox(height: 12),
                    Container(
                      decoration: BoxDecoration(
                        color: Colors.grey.shade50,
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: Colors.grey.shade200),
                      ),
                      child: ListView.separated(
                        physics: NeverScrollableScrollPhysics(),
                        shrinkWrap: true,
                        itemCount: _selectedArticles.length,
                        separatorBuilder: (context, index) => Divider(height: 1, color: Colors.grey.shade200),
                        itemBuilder: (context, index) {
                          final item = _selectedArticles[index];
                          return Padding(
                            padding: EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                            child: Row(
                              children: [
                                Expanded(
                                  flex: 5,
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        item['name'],
                                        style: TextStyle(
                                          fontFamily: 'Montserrat',
                                          fontWeight: FontWeight.w600,
                                          fontSize: 13,
                                        ),
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                      Text(
                                        "Precio: \$${item['price'].toStringAsFixed(2)}",
                                        style: TextStyle(
                                          fontFamily: 'Montserrat',
                                          fontSize: 12,
                                          color: Colors.grey.shade600,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                                SizedBox(width: 8),
                                Text(
                                  "x${item['quantity']}",
                                  style: TextStyle(
                                    fontFamily: 'Montserrat',
                                    fontWeight: FontWeight.bold,
                                    fontSize: 13,
                                  ),
                                ),
                                SizedBox(width: 8),
                                Text(
                                  "\$${item['total'].toStringAsFixed(2)}",
                                  style: TextStyle(
                                    fontFamily: 'Montserrat',
                                    fontWeight: FontWeight.bold,
                                    fontSize: 13,
                                  ),
                                ),
                              ],
                            ),
                          );
                        },
                      ),
                    ),

                    Divider(height: 24, color: Colors.grey.shade300),

                    // Totales
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          "Subtotal:",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontWeight: FontWeight.w500,
                            fontSize: 13,
                          ),
                        ),
                        Text(
                          "\$${subtotal.toStringAsFixed(2)}",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontWeight: FontWeight.w500,
                            fontSize: 13,
                          ),
                        ),
                      ],
                    ),
                    SizedBox(height: 4),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          "IVA ${ivaRate.toStringAsFixed(0)}%:",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontWeight: FontWeight.w500,
                            fontSize: 13,
                          ),
                        ),
                        Text(
                          "\$${ivaAmount.toStringAsFixed(2)}",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontWeight: FontWeight.w500,
                            fontSize: 13,
                          ),
                        ),
                      ],
                    ),
                    if (tipAmount > 0) ...[
                      SizedBox(height: 4),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                            "Propina:",
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontWeight: FontWeight.w500,
                              fontSize: 13,
                            ),
                          ),
                          Text(
                            "\$${tipAmount.toStringAsFixed(2)}",
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontWeight: FontWeight.w500,
                              fontSize: 13,
                            ),
                          ),
                        ],
                      ),
                    ],
                    SizedBox(height: 8),
                    Divider(height: 1, color: Colors.grey.shade300),
                    SizedBox(height: 8),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          "TOTAL:",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 15,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        Text(
                          "\$${grandTotal.toStringAsFixed(2)}",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 17,
                            fontWeight: FontWeight.bold,
                            color: Colors.purple,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),

              // ... Resto del código igual que antes
              SizedBox(height: 24),

              // Campos adicionales
              Text(
                "Información Adicional",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: Colors.black,
                ),
              ),
              SizedBox(height: 10),
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.grey.shade200),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Propina
                    Text(
                      "Propina",
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                        color: Colors.grey.shade700,
                      ),
                    ),
                    SizedBox(height: 8),
                    TextFormField(
                      controller: _tipController,
                      decoration: InputDecoration(
                        hintText: "0.00",
                        prefixText: "\$ ",
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(8),
                        ),
                        contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      ),
                      keyboardType: TextInputType.numberWithOptions(decimal: true),
                      onChanged: (value) {
                        setState(() {
                          // La actualización se reflejará en la próxima reconstrucción
                        });
                      },
                    ),
                    SizedBox(height: 16),

                    // Mensaje
                    Text(
                      "Mensaje",
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                        color: Colors.grey.shade700,
                      ),
                    ),
                    SizedBox(height: 8),
                    TextFormField(
                      controller: _messageController,
                      decoration: InputDecoration(
                        hintText: "Agregar un mensaje a la factura",
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(8),
                        ),
                        contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      ),
                      maxLines: 2,
                    ),
                    SizedBox(height: 16),

                    // Información adicional
                    Text(
                      "Información Adicional",
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                        color: Colors.grey.shade700,
                      ),
                    ),
                    SizedBox(height: 8),
                    TextFormField(
                      controller: _additionalInfoController,
                      decoration: InputDecoration(
                        hintText: "Añadir información adicional",
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(8),
                        ),
                        contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      ),
                      maxLines: 3,
                    ),
                  ],
                ),
              ),

              SizedBox(height: 16),

              // Nota de autorización SRI
              Container(
                padding: EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.blue.shade50,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.blue.shade200, width: 1),
                ),
                child: Row(
                  children: [
                    Icon(
                      Icons.info_outline,
                      color: Colors.blue.shade700,
                      size: 20,
                    ),
                    SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        "Al emitir esta factura, el documento será enviado al SRI para su autorización con la secuencia $nextSequence. Una vez autorizada, se enviará automáticamente al correo del cliente si ha sido proporcionado.",
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 12,
                          color: Colors.blue.shade900,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }

// Método auxiliar para construir filas de información
  Widget _buildInfoRow(String label, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 120, // Ancho fijo para etiquetas
          child: Text(
            "$label:",
            style: TextStyle(
              fontFamily: 'Montserrat',
              fontSize: 13,
              color: Colors.grey.shade700,
            ),
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontFamily: 'Montserrat',
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: Colors.black,
            ),
          ),
        ),
      ],
    );
  }



  final _additionalInfoController = TextEditingController();

// Método actualizado para preparar los datos de factura con la estructura requerida
  Future<Map<String, dynamic>> _prepareInvoiceData() async {

    final articleService = ArticleService(ApiClient());
    final now = DateTime.now().toUtc();
    final quitoTime = now.subtract(const Duration(hours: 5));
    final emissionDate = quitoTime.toIso8601String();
    final taxRate = await articleService.getTaxRateByCode(4);
    final emissionPointProvider = Provider.of<EmissionPointProvider>(context, listen: false);
    final selectedEmissionPoint = emissionPointProvider.selectedEmissionPoint;

    // Calcular la próxima secuencia
    String nextSequence = '';
    if (emissionPointProvider.lastInvoiceSequence != null) {
      String lastSequence = emissionPointProvider.lastInvoiceSequence!;
      nextSequence = emissionPointProvider.calculateNextSequence(lastSequence);
    } else if (selectedEmissionPoint != null &&
        selectedEmissionPoint['sequences'] != null &&
        (selectedEmissionPoint['sequences'] as List).isNotEmpty) {
      nextSequence = (selectedEmissionPoint['sequences'] as List).first['code'];
    }

    // Obtener datos de la empresa y sucursal desde el almacenamiento
    final enterpriseData = await _storageService.getEnterprise();
    final branchData = await _storageService.getBranch();

    if (enterpriseData == null || branchData == null) {
      throw Exception("No se pudo obtener la información de la empresa o sucursal");
    }

    // Estructurar los datos según el formato requerido
    final invoice = {
      "enterprise": {
        "idEnterprise": enterpriseData['id'],
        "companyName": enterpriseData['companyName'] ?? "",
        "comercialName": enterpriseData['comercialName'] ?? enterpriseData['companyName'] ?? "",
        "ruc": enterpriseData['ruc'] ?? "",
        "addressMatriz": enterpriseData['address'] ?? "",
        "phone": enterpriseData['phone'] ?? "",
        "email": enterpriseData['email'] ?? "info@empresa.com",
        "accountant": enterpriseData['accountant'] ?? "y"
      },
      "client": {
        "id_client": _selectedClientId,
        "razonSocial": _clientNameController.text,
        "dni": _clientIdController.text,
        "address": _clientAddressController.text,
        "phone": "099999999", // Valor predeterminado si no hay campo para teléfono
        "info": _isFinalConsumer ? "CONSUMIDOR FINAL" : "",
        "email": _clientEmailController.text.isEmpty ? "consumidorfinal@email.com" : _clientEmailController.text,
        "typeDniId": _isFinalConsumer ? 7 : (_clientIdController.text.length == 13 ? 4 : 5) // 7=Consumidor Final, 4=RUC, 5=Cédula
      },
      "emissionPoint": {
        "idEmissionPoint": selectedEmissionPoint?['idEmissionPoint'] ?? 0,
        "code": selectedEmissionPoint?['code'] ?? "",
        "details": selectedEmissionPoint?['details'] ?? ""
      },
      "branch": {
        "idBranch": branchData['id'] ?? 0,
        "code": branchData['code'] ?? "",
        "description": branchData['description'] ?? "",
        "address": branchData['address'] ?? "",
        "phone": branchData['phone'] ?? ""
      },
      "documentType": {
        "idDocumentType": 1, // ID para facturas
        "nameDocument": "FACTURA"
      },
      "sequence": {
        "idSequence": selectedEmissionPoint?['sequences'] != null &&
            (selectedEmissionPoint?['sequences'] as List).isNotEmpty ?
        (selectedEmissionPoint?['sequences'] as List).first['idSequence'] : 1,
        "code": nextSequence
      },
      "details": _selectedArticles.map((item) {
        final article = item['article'] as Article;
        final quantity = item['quantity'] as int;
        final price = item['price'] as double;
        final discount = item['discount'] as double? ?? 0.0;
        final codeStub = item['codeStub'] as String? ?? article.code;

        return {
          "codeStub": codeStub,
          "description": article.name,
          "amount": quantity,
          "discount": discount,
          "ivaPorc": taxRate, // Usar la tarifa obtenida del endpoint
          "icePorc": 0,
          "iceValor": 0,
          "note1": article.description,
          "note2": "",
          "note3": "",
          "tariffId": article.fareIds.isNotEmpty ? article.fareIds.first.id : 1,
          "articleId": article.id
        };
      }).toList(),
      "payments": [
        {
          "total": _invoiceTotal,
          "deadline": 0,
          "unitTime": "dias",
          "paymentId": 1
        }
      ],
      "emissionDate": emissionDate,
      "totalWithoutTaxes": _invoiceTotal / 1.12, // Subtotal sin IVA
      "totalDiscount": 0, // Por defecto sin descuento
      "tip": double.tryParse(_tipController.text) ?? 0.0,
      "message": _messageController.text,
      "totalAmount": _invoiceTotal,
      "currency": "USD",
      "accessKey": "string", // Se generará en el servidor
      "sequenceNumber": "${selectedEmissionPoint?['code'] ?? ""}-${nextSequence}",
      "electronicStatus": "PENDIENTE",
      "authorizationNumber": "string", // Se generará en el servidor
      "authorizationDate": DateTime.now().toUtc().toIso8601String(),
      "additionalInfo": _additionalInfoController.text.isEmpty ? "EXCELENTE SERVICIO" : _additionalInfoController.text,
      "branchId": branchData['id'] ?? 0,
      "companyId": enterpriseData['id'] ?? 0,
      "emissionPointId": selectedEmissionPoint?['idEmissionPoint'] ?? 0,
      "receiptId": 1, // ID del recibo (se puede ajustar según necesidad)
      "sequenceId": selectedEmissionPoint?['sequences'] != null &&
          (selectedEmissionPoint?['sequences'] as List).isNotEmpty ?
      (selectedEmissionPoint?['sequences'] as List).first['idSequence'] : 1
    };

    return invoice;
  }
  Future<void> _sendInvoiceToServer() async {
    try {
      // Mostrar diálogo de carga
      showDialog(
        context: context,
        barrierDismissible: false,
        builder: (context) => AlertDialog(
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              CircularProgressIndicator(color: AppColors.gold),
              SizedBox(height: 16),
              Text(
                "Procesando factura...",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                ),
              ),
            ],
          ),
        ),
      );

      // Preparar datos de la factura
      final invoiceData = await _prepareInvoiceData();

      // Crear instancia de ApiClient
      final apiClient = ApiClient();

      // Enviar datos al servidor
      final response = await apiClient.post(
        '/Invoices',
        data: invoiceData,
      );

      // Cerrar diálogo de carga
      Navigator.pop(context);

      // Verificar respuesta
      if (response.statusCode == 200 || response.statusCode == 201) {
        // Mostrar mensaje de éxito
        showDialog(
          context: context,
          builder: (context) => AlertDialog(
            // Añadir esta propiedad para asegurar que el diálogo no sea demasiado estrecho
            insetPadding: EdgeInsets.symmetric(horizontal: 20, vertical: 24),
            title: Row(
              // Añadir esta propiedad para asegurar que el texto se ajuste correctamente
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.check_circle, color: Colors.green),
                SizedBox(width: 8),
                // Envuelve el Text en un Flexible para permitir que se ajuste
                Flexible(
                  child: Text(
                    "¡Factura Emitida!",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontWeight: FontWeight.bold,
                    ),
                    // Añadir estas propiedades para controlar el desbordamiento del texto
                    overflow: TextOverflow.ellipsis,
                    softWrap: true,
                  ),
                ),
              ],
            ),
            content: Container(
              // Establecer un ancho máximo para el contenido
              width: MediaQuery.of(context).size.width * 0.8,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    "La factura ha sido emitida correctamente y enviada al SRI para su autorización.",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                    ),
                    // Añadir estas propiedades para asegurar que el texto se ajuste
                    softWrap: true,
                  ),
                  SizedBox(height: 16),
                  Text(
                    "Número de factura:",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontWeight: FontWeight.w600,
                      color: AppColors.textMedium,
                    ),
                  ),
                  SizedBox(height: 4),
                  // Envuelve este texto en un Container con overflow para números largos
                  Container(
                    width: double.infinity,
                    child: Text(
                      invoiceData["sequenceNumber"] as String,
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: AppColors.gold,
                      ),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
            ),
            actions: [
              ElevatedButton(
                onPressed: () {
                  // Cerrar diálogo y regresar a la pantalla de facturación
                  Navigator.pop(context);
                  Navigator.pop(context);
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.gold,
                  foregroundColor: Colors.white,
                ),
                child: Text(
                  "Aceptar",
                  style: TextStyle(
                    fontFamily: 'Montserrat',
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
            // Añadir esta propiedad para asegurar que el contenido se ajuste correctamente
            contentPadding: EdgeInsets.fromLTRB(24, 20, 24, 0),
            // Usar una forma personalizada si es necesario para más espacio
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(16),
            ),
          ),
        );
      } else {
        // Mostrar mensaje de error
        showDialog(
          context: context,
          builder: (context) => AlertDialog(
            title: Row(
              children: [
                Icon(Icons.error_outline, color: Colors.red),
                SizedBox(width: 8),
                Text(
                  "Error",
                  style: TextStyle(
                    fontFamily: 'Montserrat',
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            content: Text(
              "Ocurrió un error al emitir la factura. Por favor, inténtelo de nuevo.",
              style: TextStyle(
                fontFamily: 'Montserrat',
              ),
            ),
            actions: [
              ElevatedButton(
                onPressed: () {
                  Navigator.pop(context);
                },
                child: Text(
                  "Aceptar",
                  style: TextStyle(
                    fontFamily: 'Montserrat',
                    fontWeight: FontWeight.bold,
                  ),
                ),
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.red,
                  foregroundColor: Colors.white,
                ),
              ),
            ],
          ),
        );
      }
    } catch (e) {
      // Cerrar diálogo de carga si está abierto
      Navigator.of(context, rootNavigator: true).pop();

      // Mostrar mensaje de error
      showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: Row(
            children: [
              Icon(Icons.error_outline, color: Colors.red),
              SizedBox(width: 8),
              Text(
                "Error",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
          content: Text(
            "Error: $e",
            style: TextStyle(
              fontFamily: 'Montserrat',
            ),
          ),
          actions: [
            ElevatedButton(
              onPressed: () {
                Navigator.pop(context);
              },
              child: Text(
                "Aceptar",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontWeight: FontWeight.bold,
                ),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.red,
                foregroundColor: Colors.white,
              ),
            ),
          ],
        ),
      );
    }
  }

// Modificar el método _emitInvoice() para usar _sendInvoiceToServer()
  void _emitInvoice() {
    // Mostrar diálogo de confirmación
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(
          "Confirmar Facturación",
          style: TextStyle(
            fontFamily: 'Montserrat',
            fontWeight: FontWeight.bold,
          ),
        ),
        content: Text(
          "¿Está seguro de emitir esta factura?",
          style: TextStyle(
            fontFamily: 'Montserrat',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: Text(
              "Cancelar",
              style: TextStyle(
                fontFamily: 'Montserrat',
                color: AppColors.textMedium,
              ),
            ),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.pop(context); // Cerrar diálogo
              _sendInvoiceToServer(); // Enviar factura al servidor
            },
            child: Text(
              "Emitir Factura",
              style: TextStyle(
                fontFamily: 'Montserrat',
                fontWeight: FontWeight.bold,
              ),
            ),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.green.shade600,
              foregroundColor: Colors.white,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildErrorView(String error) {
    return Center(
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.error_outline,
              size: 64,
              color: Colors.red.shade700,
            ),
            SizedBox(height: 16),
            Text(
              'Error al cargar puntos de emisión',
              style: TextStyle(
                fontFamily: 'Montserrat',
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: AppColors.textDark,
              ),
            ),
            SizedBox(height: 8),
            Text(
              error,
              style: TextStyle(
                fontFamily: 'Montserrat',
                fontSize: 14,
                color: AppColors.textMedium,
              ),
              textAlign: TextAlign.center,
            ),
            SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: _initEmissionPoints,
              icon: Icon(Icons.refresh),
              label: Text(
                'Reintentar',
                style: TextStyle(fontFamily: 'Montserrat'),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.gold,
                foregroundColor: Colors.white,
                padding: EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildNoEmissionPointsView() {
    return Center(
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.info_outline,
              size: 64,
              color: Colors.blue.shade700,
            ),
            SizedBox(height: 16),
            Text(
              'No hay puntos de emisión',
              style: TextStyle(
                fontFamily: 'Montserrat',
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: AppColors.textDark,
              ),
            ),
            SizedBox(height: 8),
            Text(
              'Esta sucursal no tiene puntos de emisión configurados. Contacte al administrador para configurarlos.',
              style: TextStyle(
                fontFamily: 'Montserrat',
                fontSize: 14,
                color: AppColors.textMedium,
              ),
              textAlign: TextAlign.center,
            ),
            SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: () => Navigator.pop(context),
              icon: Icon(Icons.arrow_back),
              label: Text(
                'Volver',
                style: TextStyle(fontFamily: 'Montserrat'),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.gold,
                foregroundColor: Colors.white,
                padding: EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
