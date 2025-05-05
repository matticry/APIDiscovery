// lib/presentation/screens/create_invoice_screen.dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../config/theme/app_colors.dart';
import '../providers/enterprise_provider.dart';
import '../providers/emission_point_provider.dart';
import '../../data/services/enterprise_storage_service.dart';
import '../../data/services/article_service.dart';
import '../../core/api/api_client.dart';
import '../../domain/entities/article.dart';

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
  String _searchQuery = '';
  List<Map<String, dynamic>> _selectedArticles = [];

  // Cliente
  bool _isFinalConsumer = true;
  final _clientIdController = TextEditingController();
  final _clientNameController = TextEditingController();
  final _clientAddressController = TextEditingController();
  final _clientEmailController = TextEditingController();

  // Controladores adicionales para validación
  final _formKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    // Inicializar con valores predeterminados para consumidor final
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
      // Obtener directamente los datos de la sucursal del almacenamiento
      final branchData = await _storageService.getBranch();

      if (branchData != null && branchData.containsKey('id') &&
          branchData['id'] != null) {
        // Convertir el ID a entero si es necesario
        int branchId;
        if (branchData['id'] is String) {
          branchId = int.tryParse(branchData['id']) ?? 1;
        } else {
          branchId = branchData['id'];
        }

        // Mostrar información detallada para depuración
        print("Obteniendo puntos de emisión para la sucursal ID: $branchId");

        // Cargar los puntos de emisión para esta sucursal
        await emissionPointProvider.loadEmissionPoints(branchId);
      } else {
        // Si no se encuentra el ID de la sucursal
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

        // Usar sucursal ID 1 como predeterminado
        await emissionPointProvider.loadEmissionPoints(1);
      }
    } catch (e) {
      // Mostrar error detallado
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
        _articles = await articleService.getArticlesByEnterprise(enterpriseId);

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
    // Verificar si el artículo ya está en la lista
    final existingIndex = _selectedArticles.indexWhere((item) =>
    item['id'] == article.id);

    if (existingIndex >= 0) {
      // Si ya existe, incrementar la cantidad
      setState(() {
        _selectedArticles[existingIndex]['quantity'] += 1;
        _calculateTotal(existingIndex);
      });
    } else {
      // Si no existe, agregarlo con cantidad 1
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

    _selectedArticles[index]['total'] = price * quantity;
  }

  double get _invoiceTotal {
    if (_selectedArticles.isEmpty) return 0;
    return _selectedArticles.fold(
        0, (sum, item) => sum + (item['total'] as double));
  }

  List<Article> get _filteredArticles {
    if (_searchQuery.isEmpty) return _articles;

    return _articles.where((article) {
      return article.name.toLowerCase().contains(_searchQuery.toLowerCase()) ||
          article.code.toLowerCase().contains(_searchQuery.toLowerCase());
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    final emissionPointProvider = Provider.of<EmissionPointProvider>(context);
    final isLoading = emissionPointProvider.isLoading;
    final hasError = emissionPointProvider.error != null;
    final hasEmissionPoints = emissionPointProvider.hasEmissionPoints;

    return Scaffold(
      backgroundColor: AppColors.offWhite,
      appBar: AppBar(
        backgroundColor: AppColors.gold,
        elevation: 2,
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
        child: isLoading
            ? Center(child: CircularProgressIndicator(color: AppColors.gold))
            : hasError
            ? _buildErrorView(emissionPointProvider.error!)
            : !hasEmissionPoints
            ? _buildNoEmissionPointsView()
            : _buildStepper(),
      ),
    );
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

  Widget _buildStep1Content() {
    final emissionPointProvider = Provider.of<EmissionPointProvider>(context);
    final emissionPoints = emissionPointProvider.emissionPoints;
    final selectedEmissionPoint = emissionPointProvider.selectedEmissionPoint;

    // Variable para almacenar la próxima secuencia
    String nextSequence = '';
    if (selectedEmissionPoint != null &&
        emissionPointProvider.lastInvoiceSequence != null) {
      String lastSequence = emissionPointProvider.lastInvoiceSequence!;
      nextSequence = emissionPointProvider.calculateNextSequence(lastSequence);
    } else if (selectedEmissionPoint != null &&
        selectedEmissionPoint['sequences'] != null &&
        (selectedEmissionPoint['sequences'] as List).isNotEmpty) {
      nextSequence = (selectedEmissionPoint['sequences'] as List).first['code'];
    }

    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Sección de punto de emisión
          Container(
            padding: EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(12),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withOpacity(0.05),
                  blurRadius: 10,
                  spreadRadius: 1,
                ),
              ],
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  "Punto de Emisión",
                  style: TextStyle(
                    fontFamily: 'Montserrat',
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textDark,
                  ),
                ),
                SizedBox(height: 12),
                Container(
                  decoration: BoxDecoration(
                    border: Border.all(color: Colors.grey.shade300),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: DropdownButtonFormField<int>(
                    decoration: InputDecoration(
                      labelText: "Seleccione punto de emisión",
                      border: InputBorder.none,
                      contentPadding: EdgeInsets.symmetric(
                          horizontal: 16, vertical: 8),
                    ),
                    value: selectedEmissionPoint != null
                        ? selectedEmissionPoint['idEmissionPoint']
                        : null,
                    items: emissionPoints.map((emissionPoint) {
                      return DropdownMenuItem<int>(
                        value: emissionPoint['idEmissionPoint'],
                        child: Text(
                          "${emissionPoint['code']} - ${emissionPoint['details']}",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 14,
                          ),
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
                        final selected = emissionPoints.firstWhere(
                              (emissionPoint) =>
                          emissionPoint['idEmissionPoint'] == value,
                        );
                        emissionPointProvider.selectEmissionPoint(selected);
                      }
                    },
                  ),
                ),
                if (selectedEmissionPoint != null &&
                    selectedEmissionPoint['sequences'] != null &&
                    (selectedEmissionPoint['sequences'] as List).isNotEmpty)
                  Padding(
                    padding: EdgeInsets.only(top: 12),
                    child: Row(
                      children: [
                        Icon(
                          Icons.info_outline,
                          size: 16,
                          color: AppColors.gold,
                        ),
                        SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            "Secuencia actual: ${(selectedEmissionPoint['sequences'] as List)
                                .first['code']}",
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontSize: 12,
                              color: AppColors.textMedium,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),

                // Mostrar la alerta con la última secuencia si existe
                if (selectedEmissionPoint != null &&
                    emissionPointProvider.lastInvoiceSequence != null) ...[
                  SizedBox(height: 16),
                  Container(
                    padding: EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Colors.amber.shade50,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(
                          color: Colors.amber.shade200, width: 1),
                    ),
                    child: Row(
                      children: [
                        Icon(
                          Icons.receipt_long,
                          color: AppColors.gold,
                          size: 24,
                        ),
                        SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                "Información de Secuencia",
                                style: TextStyle(
                                  fontFamily: 'Montserrat',
                                  fontSize: 14,
                                  fontWeight: FontWeight.bold,
                                  color: AppColors.gold,
                                ),
                              ),
                              SizedBox(height: 4),
                              Text(
                                "Última secuencia emitida: ${emissionPointProvider
                                    .lastInvoiceSequence}",
                                style: TextStyle(
                                  fontFamily: 'Montserrat',
                                  fontSize: 13,
                                  color: Colors.amber.shade900,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ],

                // Mostrar campo con la próxima secuencia a utilizar
                if (selectedEmissionPoint != null && !nextSequence.isEmpty) ...[
                  SizedBox(height: 16),
                  Text(
                    "Secuencia a utilizar",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontSize: 14,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textDark,
                    ),
                  ),
                  SizedBox(height: 8),
                  TextFormField(
                    initialValue: nextSequence,
                    readOnly: true,
                    decoration: InputDecoration(
                      filled: true,
                      fillColor: Colors.grey.shade100,
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(8),
                        borderSide: BorderSide(color: Colors.grey.shade300),
                      ),
                      contentPadding: EdgeInsets.symmetric(
                          horizontal: 16, vertical: 12),
                      prefixIcon: Icon(Icons.pin, color: AppColors.gold),
                      suffixIcon: Icon(
                          Icons.lock_outline, color: Colors.grey.shade500),
                    ),
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontWeight: FontWeight.bold,
                      color: AppColors.gold,
                    ),
                  ),
                ],
              ],
            ),
          ),

          SizedBox(height: 24),

          // Datos del cliente
          Container(
            padding: EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(12),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withOpacity(0.05),
                  blurRadius: 10,
                  spreadRadius: 1,
                ),
              ],
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  "Datos del Cliente",
                  style: TextStyle(
                    fontFamily: 'Montserrat',
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textDark,
                  ),
                ),
                SizedBox(height: 16),

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
                            color: _isFinalConsumer ? AppColors.gold
                                .withOpacity(0.1) : Colors.transparent,
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(
                              color: _isFinalConsumer ? AppColors.gold : Colors
                                  .grey.shade300,
                              width: 1,
                            ),
                          ),
                          child: Column(
                            children: [
                              Icon(
                                Icons.person_outline,
                                color: _isFinalConsumer
                                    ? AppColors.gold
                                    : Colors.grey.shade600,
                                size: 24,
                              ),
                              SizedBox(height: 8),
                              Text(
                                "Consumidor Final",
                                style: TextStyle(
                                  fontFamily: 'Montserrat',
                                  color: _isFinalConsumer
                                      ? AppColors.gold
                                      : Colors.grey.shade600,
                                  fontWeight: _isFinalConsumer
                                      ? FontWeight.bold
                                      : FontWeight.normal,
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
                            color: !_isFinalConsumer ? AppColors.gold
                                .withOpacity(0.1) : Colors.transparent,
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(
                              color: !_isFinalConsumer ? AppColors.gold : Colors
                                  .grey.shade300,
                              width: 1,
                            ),
                          ),
                          child: Column(
                            children: [
                              Icon(
                                Icons.business,
                                color: !_isFinalConsumer
                                    ? AppColors.gold
                                    : Colors.grey.shade600,
                                size: 24,
                              ),
                              SizedBox(height: 8),
                              Text(
                                "Con Datos",
                                style: TextStyle(
                                  fontFamily: 'Montserrat',
                                  color: !_isFinalConsumer
                                      ? AppColors.gold
                                      : Colors.grey.shade600,
                                  fontWeight: !_isFinalConsumer ? FontWeight
                                      .bold : FontWeight.normal,
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
                  TextFormField(
                    controller: _clientIdController,
                    decoration: InputDecoration(
                      labelText: "RUC / Cédula",
                      prefixIcon: Icon(Icons.badge_outlined),
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                    ),
                    validator: (value) {
                      if (value == null || value.isEmpty) {
                        return 'Por favor ingrese el RUC o cédula';
                      }
                      // Validar que sea un RUC o cédula válido
                      if (value.length != 10 && value.length != 13) {
                        return 'Ingrese un RUC o cédula válido';
                      }
                      return null;
                    },
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
                        final emailRegExp = RegExp(
                            r'^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$');
                        if (!emailRegExp.hasMatch(value)) {
                          return 'Por favor ingrese un email válido';
                        }
                      }
                      return null;
                    },
                  ),
                ] else
                  ...[
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
                            color: AppColors.textMedium,
                            size: 20,
                          ),
                          SizedBox(width: 12),
                          Expanded(
                            child: Text(
                              "Se utilizarán los datos predeterminados para Consumidor Final",
                              style: TextStyle(
                                fontFamily: 'Montserrat',
                                fontSize: 14,
                                color: AppColors.textMedium,
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
        ],
      ),
    );
  }

  Widget _buildStep2Content() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Buscador de productos
        Container(
          padding: EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(12),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.05),
                blurRadius: 10,
                spreadRadius: 1,
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                "Buscar Productos",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textDark,
                ),
              ),
              SizedBox(height: 12),
              TextField(
                decoration: InputDecoration(
                  hintText: "Buscar por nombre o código...",
                  prefixIcon: Icon(Icons.search),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
                onChanged: (value) {
                  setState(() {
                    _searchQuery = value;
                  });
                },
              ),
              SizedBox(height: 16),
              _isLoadingArticles
                  ? Center(
                  child: CircularProgressIndicator(color: AppColors.gold))
                  : _filteredArticles.isEmpty
                  ? Center(
                child: Text(
                  "No se encontraron productos",
                  style: TextStyle(
                    fontFamily: 'Montserrat',
                    fontSize: 14,
                    color: AppColors.textMedium,
                  ),
                ),
              )
                  : Container(
                height: 200,
                decoration: BoxDecoration(
                  border: Border.all(color: Colors.grey.shade200),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: ListView.separated(
                  padding: EdgeInsets.all(8),
                  itemCount: _filteredArticles.length,
                  separatorBuilder: (context, index) => Divider(height: 1),
                  itemBuilder: (context, index) {
                    final article = _filteredArticles[index];
                    return ListTile(
                      contentPadding: EdgeInsets.symmetric(
                          horizontal: 8, vertical: 4),
                      title: Text(
                        article.name,
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      subtitle: Text(
                        "Código: ${article.code} - Precio: \$${article.priceUnit
                            .toStringAsFixed(2)} - Stock: ${article.stock}",
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 12,
                        ),
                      ),
                      trailing: IconButton(
                        icon: Icon(Icons.add_circle, color: AppColors.gold),
                        onPressed: () => _addArticleToInvoice(article),
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
        Container(
          padding: EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(12),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.05),
                blurRadius: 10,
                spreadRadius: 1,
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                "Productos Seleccionados",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textDark,
                ),
              ),
              SizedBox(height: 12),
              _selectedArticles.isEmpty
                  ? Center(
                child: Padding(
                  padding: EdgeInsets.all(16),
                  child: Text(
                    "No hay productos agregados",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontSize: 14,
                      color: AppColors.textMedium,
                      fontStyle: FontStyle.italic,
                    ),
                  ),
                ),
              )
                  : Column(
                children: [
                  // Encabezados
                  Padding(
                    padding: EdgeInsets.symmetric(horizontal: 8, vertical: 8),
                    child: Row(
                      children: [
                        Expanded(
                          flex: 3,
                          child: Text(
                            "Producto",
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                              color: AppColors.textMedium,
                            ),
                          ),
                        ),
                        Expanded(
                          flex: 1,
                          child: Text(
                            "Precio",
                            textAlign: TextAlign.center,
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                              color: AppColors.textMedium,
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
                              color: AppColors.textMedium,
                            ),
                          ),
                        ),
                        Expanded(
                          flex: 1,
                          child: Text(
                            "Total",
                            textAlign: TextAlign.center,
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                              color: AppColors.textMedium,
                            ),
                          ),
                        ),
                        SizedBox(width: 36),
                        // Espacio para el botón de eliminar
                      ],
                    ),
                  ),
                  Divider(height: 1, color: Colors.grey.shade300),
                  // Lista de productos
                  Container(
                    constraints: BoxConstraints(maxHeight: 250),
                    child: ListView.builder(
                      shrinkWrap: true,
                      itemCount: _selectedArticles.length,
                      itemBuilder: (context, index) {
                        final item = _selectedArticles[index];
                        return Column(
                          children: [
                            Padding(
                              padding: EdgeInsets.symmetric(
                                  horizontal: 8, vertical: 8),
                              child: Row(
                                children: [
                                  Expanded(
                                    flex: 3,
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment
                                          .start,
                                      children: [
                                        Text(
                                          item['name'],
                                          style: TextStyle(
                                            fontFamily: 'Montserrat',
                                            fontWeight: FontWeight.w600,
                                          ),
                                        ),
                                        Text(
                                          "Cód: ${item['code']}",
                                          style: TextStyle(
                                            fontFamily: 'Montserrat',
                                            fontSize: 12,
                                            color: AppColors.textMedium,
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                  Expanded(
                                    flex: 1,
                                    child: Text(
                                      "\$${item['price'].toStringAsFixed(2)}",
                                      textAlign: TextAlign.center,
                                      style: TextStyle(
                                        fontFamily: 'Montserrat',
                                        fontSize: 14,
                                      ),
                                    ),
                                  ),
                                  Expanded(
                                    flex: 1,
                                    child: Row(
                                      mainAxisAlignment: MainAxisAlignment
                                          .center,
                                      children: [
                                        InkWell(
                                          onTap: () =>
                                              _updateQuantity(
                                              index, item['quantity'] - 1),
                                          child: Container(
                                            padding: EdgeInsets.all(2),
                                            decoration: BoxDecoration(
                                              color: Colors.grey.shade200,
                                              borderRadius: BorderRadius
                                                  .circular(4),
                                            ),
                                            child: Icon(Icons.remove, size: 16),
                                          ),
                                        ),
                                        Container(
                                          padding: EdgeInsets.symmetric(
                                              horizontal: 8),
                                          child: Text(
                                            "${item['quantity']}",
                                            style: TextStyle(
                                              fontFamily: 'Montserrat',
                                              fontWeight: FontWeight.bold,
                                            ),
                                          ),
                                        ),
                                        InkWell(
                                          onTap: () =>
                                              _updateQuantity(
                                              index, item['quantity'] + 1),
                                          child: Container(
                                            padding: EdgeInsets.all(2),
                                            decoration: BoxDecoration(
                                              color: AppColors.gold.withOpacity(
                                                  0.2),
                                              borderRadius: BorderRadius
                                                  .circular(4),
                                            ),
                                            child: Icon(Icons.add, size: 16,
                                                color: AppColors.gold),
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                  Expanded(
                                    flex: 1,
                                    child: Text(
                                      "\$${item['total'].toStringAsFixed(2)}",
                                      textAlign: TextAlign.center,
                                      style: TextStyle(
                                        fontFamily: 'Montserrat',
                                        fontWeight: FontWeight.bold,
                                      ),
                                    ),
                                  ),
                                  IconButton(
                                    icon: Icon(Icons.delete_outline,
                                        color: Colors.red.shade400, size: 20),
                                    onPressed: () => _removeArticle(index),
                                    padding: EdgeInsets.zero,
                                    constraints: BoxConstraints(),
                                  ),
                                ],
                              ),
                            ),
                            Divider(height: 1, color: Colors.grey.shade200),
                          ],
                        );
                      },
                    ),
                  ),

                  // Total
                  Padding(
                    padding: EdgeInsets.symmetric(horizontal: 8, vertical: 16),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        Text(
                          "Total:",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        SizedBox(width: 16),
                        Text(
                          "\$${_invoiceTotal.toStringAsFixed(2)}",
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: AppColors.gold,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildStep3Content() {
    final emissionPointProvider = Provider.of<EmissionPointProvider>(context);
    final selectedEmissionPoint = emissionPointProvider.selectedEmissionPoint;
    String emissionPointCode = selectedEmissionPoint != null
        ? selectedEmissionPoint['code']
        : '';

    // Construir número de factura
    String invoiceNumber = '';
    String nextSequence = '';

    if (selectedEmissionPoint != null) {
      // Verificar si tenemos la última secuencia para calcular la siguiente
      if (emissionPointProvider.lastInvoiceSequence != null) {
        String lastSequence = emissionPointProvider.lastInvoiceSequence!;
        nextSequence =
            emissionPointProvider.calculateNextSequence(lastSequence);
        invoiceNumber = '$emissionPointCode-$nextSequence';
      }
      // Si no hay lastSequence, usar la secuencia original del punto de emisión
      else if (selectedEmissionPoint['sequences'] != null &&
          (selectedEmissionPoint['sequences'] as List).isNotEmpty) {
        final sequence = (selectedEmissionPoint['sequences'] as List)
            .first['code'];
        invoiceNumber = '$emissionPointCode-$sequence';
        nextSequence = sequence;
      }
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          padding: EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(12),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.05),
                blurRadius: 10,
                spreadRadius: 1,
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                "Resumen de Factura",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textDark,
                ),
              ),
              SizedBox(height: 16),

              // Datos de la factura
              _buildInfoItem("Punto de Emisión", emissionPointCode),
              _buildInfoItem("Número de Factura", invoiceNumber),
              if (emissionPointProvider.lastInvoiceSequence != null)
                _buildInfoItem("Última Secuencia",
                    emissionPointProvider.lastInvoiceSequence!),
              _buildInfoItem("Nueva Secuencia", nextSequence),
              _buildInfoItem("Fecha", "${DateTime
                  .now()
                  .day}/${DateTime
                  .now()
                  .month}/${DateTime
                  .now()
                  .year}"),

              Divider(height: 24, color: Colors.grey.shade300),

              // Datos del cliente
              Text(
                "Datos del Cliente",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textDark,
                ),
              ),
              SizedBox(height: 12),
              _buildInfoItem("Identificación", _clientIdController.text),
              _buildInfoItem(
                  "Nombre / Razón Social", _clientNameController.text),
              _buildInfoItem("Dirección", _clientAddressController.text),
              if (_clientEmailController.text.isNotEmpty)
                _buildInfoItem("Email", _clientEmailController.text),

              Divider(height: 24, color: Colors.grey.shade300),

              // Resumen de productos
              Text(
                "Productos",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textDark,
                ),
              ),
              SizedBox(height: 12),
              _buildInfoItem("Cantidad de Productos",
                  "${_selectedArticles.length} productos"),
              _buildInfoItem("Total de Ítems", "${_selectedArticles.fold(
                  0.0, (sum, item) => sum + item['quantity'])} items"),
              Divider(height: 24, color: Colors.grey.shade300),

              // Totales
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    "Subtotal:",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  Text(
                    "\$${(_invoiceTotal / 1.12).toStringAsFixed(2)}",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
              SizedBox(height: 8),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    "IVA 12%:",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  Text(
                    "\$${(_invoiceTotal - (_invoiceTotal / 1.12))
                        .toStringAsFixed(2)}",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
              SizedBox(height: 8),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    "TOTAL:",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  Text(
                    "\$${_invoiceTotal.toStringAsFixed(2)}",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      color: AppColors.gold,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),

        SizedBox(height: 16),

        // Notas y términos
        Container(
          padding: EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: Colors.blue.shade50,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: Colors.blue.shade200, width: 1),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(
                    Icons.info_outline,
                    color: Colors.blue.shade700,
                    size: 20,
                  ),
                  SizedBox(width: 8),
                  Text(
                    "Información importante",
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: Colors.blue.shade700,
                    ),
                  ),
                ],
              ),
              SizedBox(height: 8),
              Text(
                "Al emitir esta factura, el documento será enviado al SRI para su autorización con la secuencia ${nextSequence}. Una vez autorizada, se enviará automáticamente al correo del cliente si ha sido proporcionado.",
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 12,
                  color: Colors.blue.shade900,
                ),
              ),
            ],
          ),
        ),

        // Sección destacada para mostrar información de secuencia
        if (emissionPointProvider.lastInvoiceSequence != null) ...[
          SizedBox(height: 16),
          Container(
            padding: EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: Colors.amber.shade50,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: Colors.amber.shade200, width: 1),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Icon(
                      Icons.receipt_long,
                      color: AppColors.gold,
                      size: 20,
                    ),
                    SizedBox(width: 8),
                    Text(
                      "Información de Secuencia",
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: AppColors.gold,
                      ),
                    ),
                  ],
                ),
                SizedBox(height: 8),
                Text(
                  "Se utilizará la secuencia ${nextSequence} para esta factura, calculada automáticamente a partir de la última secuencia emitida (${emissionPointProvider
                      .lastInvoiceSequence}).",
                  style: TextStyle(
                    fontFamily: 'Montserrat',
                    fontSize: 12,
                    color: Colors.amber.shade900,
                  ),
                ),
              ],
            ),
          ),
        ],
      ],
    );
  }

  Widget _buildInfoItem(String label, String value) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            flex: 2,
            child: Text(
              "$label:",
              style: TextStyle(
                fontFamily: 'Montserrat',
                fontSize: 14,
                color: AppColors.textMedium,
              ),
            ),
          ),
          Expanded(
            flex: 3,
            child: Text(
              value,
              style: TextStyle(
                fontFamily: 'Montserrat',
                fontSize: 14,
                fontWeight: FontWeight.w600,
                color: AppColors.textDark,
              ),
            ),
          ),
        ],
      ),
    );
  }

  void _emitInvoice() {
    // Aquí iría la lógica para emitir la factura

    // Mostrar diálogo de confirmación
    showDialog(
      context: context,
      builder: (context) =>
          AlertDialog(
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

                  // Mostrar diálogo de carga
                  showDialog(
                    context: context,
                    barrierDismissible: false,
                    builder: (context) =>
                        AlertDialog(
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

                  // Simular proceso de facturación
                  Future.delayed(Duration(seconds: 2), () {
                    Navigator.pop(context); // Cerrar diálogo de carga

                    // Mostrar confirmación de éxito
                    showDialog(
                      context: context,
                      builder: (context) =>
                          AlertDialog(
                            title: Row(
                              children: [
                                Icon(Icons.check_circle, color: Colors.green),
                                SizedBox(width: 8),
                                Text(
                                  "¡Factura Emitida!",
                                  style: TextStyle(
                                    fontFamily: 'Montserrat',
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ],
                            ),
                            content: Text(
                              "La factura ha sido emitida correctamente y enviada al SRI para su autorización.",
                              style: TextStyle(
                                fontFamily: 'Montserrat',
                              ),
                            ),
                            actions: [
                              ElevatedButton(
                                onPressed: () {
                                  // Cerrar diálogo y regresar a la pantalla de facturación
                                  Navigator.pop(context);
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
                                  backgroundColor: AppColors.gold,
                                  foregroundColor: Colors.white,
                                ),
                              ),
                            ],
                          ),
                    );
                  });
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
