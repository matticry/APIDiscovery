// lib/presentation/screens/create_article_screen.dart
import 'dart:math';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../config/theme/app_colors.dart';
import '../../domain/entities/fare.dart';
import '../providers/article_provider.dart';
import '../providers/category_provider.dart';
import '../providers/enterprise_provider.dart';

class CreateArticleScreen extends StatefulWidget {
  const CreateArticleScreen({Key? key}) : super(key: key);

  @override
  _CreateArticleScreenState createState() => _CreateArticleScreenState();
}

class _CreateArticleScreenState extends State<CreateArticleScreen> {
  final _formKey = GlobalKey<FormState>();

  // Form controllers
  final _nameController = TextEditingController();
  final _codeController = TextEditingController();
  final _priceController = TextEditingController();
  final _stockController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _imageController = TextEditingController();

  // Selected values
  int? _selectedCategoryId;
  Fare? _selectedFare;
  String _status = 'A';
  int? _enterpriseId;

  // Nueva variable para controlar si se incluye imagen
  bool _includeImage = false;

  String _articleType = 'N';

  // Variable para controlar la generación automática de código
  bool _autoGenerateCode = true;

  bool _isLoading = false;
  bool _dataLoaded = false;

  @override
  void initState() {
    super.initState();

    // Setup listener para generar código automáticamente cuando cambia el nombre
    _nameController.addListener(() {
      if (_autoGenerateCode && _nameController.text.isNotEmpty) {
        _codeController.text = _generateProductCode(_nameController.text);
      }
    });

    // AGREGAR ESTE LISTENER para actualizar el preview cuando cambie el precio
    _priceController.addListener(() {
      setState(() {
        // Trigger rebuild para actualizar el preview
      });
    });

    _updateStockBasedOnType();

    // Load enterprise ID and categories when screen initializes
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadInitialData();
    });
  }

  void _updateStockBasedOnType() {
    if (_articleType == 'S') {
      // Si es servicio, establecer stock a 999999 y deshabilitar el campo
      _stockController.text = '999999';
    } else {
      // Si es normal, limpiar el campo para que el usuario ingrese
      if (_stockController.text == '999999') {
        _stockController.text = '';
      }
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _codeController.dispose();
    _priceController.dispose();
    _stockController.dispose();
    _descriptionController.dispose();
    _imageController.dispose();
    super.dispose();
  }

  // Método para generar código de producto automáticamente
  String _generateProductCode(String productName) {
    final now = DateTime.now();
    final _ = '${now.year}${now.month.toString().padLeft(2, '0')}${now.day.toString().padLeft(2, '0')}';

    // Tomar las primeras letras del nombre del producto (hasta 4)
    final words = productName.trim().split(' ');
    String prefix = words.map((word) => word.isNotEmpty ? word[0].toUpperCase() : '').join('');
    prefix = prefix.substring(0, min(prefix.length, 4));

    // Usar un número aleatorio entre 1000-9999
    final randomNum = Random().nextInt(9000) + 1000;

    return '$prefix-$randomNum';
  }

  Future<void> _loadInitialData() async {
    setState(() {
      _isLoading = true;
    });

    try {
      // Load enterprise data
      final enterpriseProvider = Provider.of<EnterpriseProvider>(context, listen: false);
      final enterpriseData = await enterpriseProvider.getEnterpriseData();

      if (enterpriseData != null && enterpriseData['id'] != null) {
        _enterpriseId = enterpriseData['id'];

        // Load categories for this enterprise
        await Provider.of<CategoryProvider>(context, listen: false)
            .loadCategories();

        // Cargar tarifas
        await Provider.of<ArticleProvider>(context, listen: false)
            .loadFares();

        // Generar un código de ejemplo
        _codeController.text = _generateProductCode("Producto Nuevo");
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'No se pudo obtener información de la empresa',
              style: TextStyle(fontFamily: 'Montserrat'),
            ),
            backgroundColor: Colors.red,
          ),
        );
      }

      setState(() {
        _isLoading = false;
        _dataLoaded = true;
      });
    } catch (e) {
      setState(() {
        _isLoading = false;
      });

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Error: $e',
            style: TextStyle(fontFamily: 'Montserrat'),
          ),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  Future<void> _saveArticle() async {
    if (_formKey.currentState!.validate()) {
      if (_selectedCategoryId == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'Por favor selecciona una categoría',
              style: TextStyle(fontFamily: 'Montserrat'),
            ),
            backgroundColor: Colors.red,
          ),
        );
        return;
      }

      if (_selectedFare == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'Por favor selecciona una tarifa',
              style: TextStyle(fontFamily: 'Montserrat'),
            ),
            backgroundColor: Colors.red,
          ),
        );
        return;
      }

      if (_enterpriseId == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'Error: No se ha podido obtener el ID de la empresa',
              style: TextStyle(fontFamily: 'Montserrat'),
            ),
            backgroundColor: Colors.red,
          ),
        );
        return;
      }

      setState(() {
        _isLoading = true;
      });

      try {
        // Create a new article object exactly matching the expected format
        final Map<String, dynamic> newArticle = {
          "name": _nameController.text.trim(),
          "code": _codeController.text.trim(),
          "priceUnit": double.parse(_priceController.text),
          "stock": _articleType == 'S' ? 999999 : int.parse(_stockController.text),
          "description": _descriptionController.text.trim(),
          "idEnterprise": _enterpriseId,
          "type": _articleType,
          "idCategory": _selectedCategoryId,
          "fareIds": [_selectedFare!.id]
        };

        // Solo añadir imagen si se incluye
        if (_includeImage && _imageController.text.isNotEmpty) {
          newArticle["image"] = _imageController.text.trim();
        }

        // Log the data being sent
        print("Creating article with data: $newArticle");

        // Save the article
        final success = await Provider.of<ArticleProvider>(context, listen: false)
            .createArticle(newArticle);

        setState(() {
          _isLoading = false;
        });

        if (success) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                'Producto creado exitosamente',
                style: TextStyle(fontFamily: 'Montserrat'),
              ),
              backgroundColor: Colors.green,
            ),
          );

          Navigator.pop(context, true);
        } else {
          String errorMessage = Provider.of<ArticleProvider>(context, listen: false).error ??
              'Error al crear el producto';

          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                errorMessage,
                style: TextStyle(fontFamily: 'Montserrat'),
              ),
              backgroundColor: Colors.red,
            ),
          );
        }
      } catch (e) {
        setState(() {
          _isLoading = false;
        });

        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'Error: $e',
              style: TextStyle(fontFamily: 'Montserrat'),
            ),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.offWhite,
      appBar: AppBar(
        backgroundColor: AppColors.gold,
        title: Text(
          _articleType == 'S' ? 'Crear Servicio' : 'Crear Producto',
          style: TextStyle(
            fontFamily: 'Montserrat',
            fontWeight: FontWeight.bold,
            color: Colors.white,
          ),
        ),
        elevation: 0,
      ),
      body: _isLoading
          ? Center(
        child: CircularProgressIndicator(
          color: AppColors.gold,
        ),
      )
          : !_dataLoaded
          ? Center(
        child: Text(
          'Cargando datos...',
          style: TextStyle(
            fontFamily: 'Montserrat',
            fontSize: 16,
            color: AppColors.textMedium,
          ),
        ),
      )
          : SingleChildScrollView(
        padding: EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // NUEVO: Tipo de artículo selector
              Container(
                width: double.infinity,
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.grey.shade300),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withOpacity(0.05),
                      blurRadius: 5,
                      offset: Offset(0, 2),
                    ),
                  ],
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Tipo de Artículo *',
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textDark,
                      ),
                    ),
                    SizedBox(height: 12),
                    Row(
                      children: [
                        Expanded(
                          child: InkWell(
                            onTap: () {
                              setState(() {
                                _articleType = 'N';
                                _updateStockBasedOnType();
                                // Regenerar código si está en modo automático
                                if (_autoGenerateCode && _nameController.text.isNotEmpty) {
                                  _codeController.text = _generateProductCode(_nameController.text);
                                }
                              });
                            },
                            child: Container(
                              padding: EdgeInsets.all(16),
                              decoration: BoxDecoration(
                                color: _articleType == 'N'
                                    ? Colors.blue.shade50
                                    : Colors.grey.shade50,
                                borderRadius: BorderRadius.circular(8),
                                border: Border.all(
                                  color: _articleType == 'N'
                                      ? Colors.blue.shade300
                                      : Colors.grey.shade300,
                                  width: 2,
                                ),
                              ),
                              child: Column(
                                children: [
                                  Icon(
                                    Icons.inventory_2,
                                    color: _articleType == 'N'
                                        ? Colors.blue.shade700
                                        : Colors.grey.shade600,
                                    size: 32,
                                  ),
                                  SizedBox(height: 8),
                                  Text(
                                    'Producto Normal',
                                    style: TextStyle(
                                      fontFamily: 'Montserrat',
                                      fontWeight: FontWeight.bold,
                                      color: _articleType == 'N'
                                          ? Colors.blue.shade700
                                          : Colors.grey.shade600,
                                    ),
                                    textAlign: TextAlign.center,
                                  ),
                                  SizedBox(height: 4),
                                  Text(
                                    'Artículo físico con inventario',
                                    style: TextStyle(
                                      fontFamily: 'Montserrat',
                                      fontSize: 12,
                                      color: _articleType == 'N'
                                          ? Colors.blue.shade600
                                          : Colors.grey.shade500,
                                    ),
                                    textAlign: TextAlign.center,
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                        SizedBox(width: 16),
                        Expanded(
                          child: InkWell(
                            onTap: () {
                              setState(() {
                                _articleType = 'S';
                                _updateStockBasedOnType();
                                // Regenerar código si está en modo automático
                                if (_autoGenerateCode && _nameController.text.isNotEmpty) {
                                  _codeController.text = _generateProductCode(_nameController.text);
                                }
                              });
                            },
                            child: Container(
                              padding: EdgeInsets.all(16),
                              decoration: BoxDecoration(
                                color: _articleType == 'S'
                                    ? Colors.green.shade50
                                    : Colors.grey.shade50,
                                borderRadius: BorderRadius.circular(8),
                                border: Border.all(
                                  color: _articleType == 'S'
                                      ? Colors.green.shade300
                                      : Colors.grey.shade300,
                                  width: 2,
                                ),
                              ),
                              child: Column(
                                children: [
                                  Icon(
                                    Icons.build,
                                    color: _articleType == 'S'
                                        ? Colors.green.shade700
                                        : Colors.grey.shade600,
                                    size: 32,
                                  ),
                                  SizedBox(height: 8),
                                  Text(
                                    'Servicio',
                                    style: TextStyle(
                                      fontFamily: 'Montserrat',
                                      fontWeight: FontWeight.bold,
                                      color: _articleType == 'S'
                                          ? Colors.green.shade700
                                          : Colors.grey.shade600,
                                    ),
                                    textAlign: TextAlign.center,
                                  ),
                                  SizedBox(height: 4),
                                  Text(
                                    'Servicio sin inventario físico',
                                    style: TextStyle(
                                      fontFamily: 'Montserrat',
                                      fontSize: 12,
                                      color: _articleType == 'S'
                                          ? Colors.green.shade600
                                          : Colors.grey.shade500,
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
                  ],
                ),
              ),

              SizedBox(height: 24),

              // Product name
              TextFormField(
                controller: _nameController,
                decoration: InputDecoration(
                  labelText: _articleType == 'S'
                      ? 'Nombre del Servicio *'
                      : 'Nombre del Producto *',
                  hintText: _articleType == 'S'
                      ? 'Ej: Consultoría técnica'
                      : 'Ej: Smartphone Samsung Galaxy',
                  prefixIcon: Icon(_articleType == 'S' ? Icons.build : Icons.shopping_bag_outlined),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  filled: true,
                  fillColor: Colors.white,
                  floatingLabelBehavior: FloatingLabelBehavior.always,
                ),
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 16,
                ),
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Por favor ingresa el nombre del ${_articleType == 'S' ? 'servicio' : 'producto'}';
                  }
                  return null;
                },
              ),

              SizedBox(height: 16),

              // Product code with auto-generation option
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: TextFormField(
                          controller: _codeController,
                          decoration: InputDecoration(
                            labelText: _articleType == 'S'
                                ? 'Código del Servicio *'
                                : 'Código del Producto *',
                            hintText: _articleType == 'S'
                                ? 'Ej: SRV-001'
                                : 'Ej: PROD-001',
                            prefixIcon: Icon(Icons.qr_code),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12),
                            ),
                            filled: true,
                            fillColor: Colors.white,
                            floatingLabelBehavior: FloatingLabelBehavior.always,
                          ),
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 16,
                          ),
                          validator: (value) {
                            if (value == null || value.trim().isEmpty) {
                              return 'Por favor ingresa el código del ${_articleType == 'S' ? 'servicio' : 'producto'}';
                            }
                            return null;
                          },
                          enabled: !_autoGenerateCode,
                        ),
                      ),
                    ],
                  ),
                  Row(
                    children: [
                      Checkbox(
                        value: _autoGenerateCode,
                        activeColor: AppColors.gold,
                        onChanged: (bool? value) {
                          setState(() {
                            _autoGenerateCode = value ?? false;
                            if (_autoGenerateCode && _nameController.text.isNotEmpty) {
                              _codeController.text = _generateProductCode(_nameController.text);
                            }
                          });
                        },
                      ),
                      Text(
                        'Generar código automáticamente',
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 14,
                          color: AppColors.textMedium,
                        ),
                      ),
                    ],
                  ),
                ],
              ),

              SizedBox(height: 16),

              // Price and Stock with different behavior based on type
              Row(
                children: [
                  // Price
                  Expanded(
                    child: TextFormField(
                      controller: _priceController,
                      keyboardType: TextInputType.numberWithOptions(decimal: true),
                      decoration: InputDecoration(
                        labelText: _articleType == 'S'
                            ? 'Precio del Servicio *'
                            : 'Precio Unitario *',
                        hintText: 'Ej: 19.99',
                        prefixIcon: Icon(Icons.attach_money),
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                        filled: true,
                        fillColor: Colors.white,
                        floatingLabelBehavior: FloatingLabelBehavior.always,
                      ),
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 16,
                      ),
                      validator: (value) {
                        if (value == null || value.isEmpty) {
                          return 'Ingresa el precio';
                        }
                        if (double.tryParse(value) == null) {
                          return 'Precio inválido';
                        }
                        if (double.parse(value) <= 0) {
                          return 'El precio debe ser mayor a 0';
                        }
                        return null;
                      },
                    ),
                  ),

                  SizedBox(width: 16),

                  // Stock - Different behavior based on type
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        TextFormField(
                          controller: _stockController,
                          keyboardType: TextInputType.number,
                          enabled: _articleType != 'S', // Disabled for services
                          decoration: InputDecoration(
                            labelText: _articleType == 'S'
                                ? 'Stock (Automático)'
                                : 'Stock Inicial *',
                            hintText: _articleType == 'S'
                                ? 'Ilimitado'
                                : 'Ej: 100',
                            prefixIcon: Icon(
                              _articleType == 'S' ? Icons.all_inclusive : Icons.inventory,
                              color: _articleType == 'S' ? Colors.green : null,
                            ),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12),
                            ),
                            filled: true,
                            fillColor: _articleType == 'S'
                                ? Colors.green.shade50
                                : Colors.white,
                            floatingLabelBehavior: FloatingLabelBehavior.always,
                          ),
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 16,
                            color: _articleType == 'S' ? Colors.green.shade700 : null,
                          ),
                          validator: (value) {
                            if (_articleType == 'S') return null; // No validation for services

                            if (value == null || value.isEmpty) {
                              return 'Ingresa el stock';
                            }
                            if (int.tryParse(value) == null) {
                              return 'Stock inválido';
                            }
                            if (int.parse(value) < 0) {
                              return 'El stock no puede ser negativo';
                            }
                            return null;
                          },
                        ),
                        if (_articleType == 'S') ...[
                          SizedBox(height: 4),
                          Text(
                            'Los servicios tienen stock ilimitado',
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontSize: 11,
                              color: Colors.green.shade600,
                              fontStyle: FontStyle.italic,
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                ],
              ),

              SizedBox(height: 16),

              // Fare Selection
              Text(
                'Tarifa *',
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textDark,
                ),
              ),
              SizedBox(height: 8),
              Consumer<ArticleProvider>(
                builder: (context, articleProvider, child) {
                  if (articleProvider.loadingFares) {
                    return CircularProgressIndicator(
                      color: AppColors.gold,
                    );
                  }

                  if (articleProvider.fares.isEmpty) {
                    return Container(
                      padding: EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: Colors.red.shade50,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: Colors.red.shade200),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'No hay tarifas disponibles',
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: Colors.red.shade700,
                            ),
                          ),
                          SizedBox(height: 8),
                          Text(
                            'No se pudieron cargar las tarifas del sistema.',
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontSize: 14,
                              color: Colors.red.shade700,
                            ),
                          ),
                        ],
                      ),
                    );
                  }

                  return Container(
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: Colors.grey.shade300),
                    ),
                    child: DropdownButtonHideUnderline(
                      child: DropdownButton<Fare>(
                        value: _selectedFare,
                        isExpanded: true,
                        hint: Padding(
                          padding: EdgeInsets.symmetric(horizontal: 16),
                          child: Text(
                            'Selecciona una tarifa',
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontSize: 16,
                              color: Colors.grey.shade600,
                            ),
                          ),
                        ),
                        padding: EdgeInsets.symmetric(horizontal: 16),
                        borderRadius: BorderRadius.circular(12),
                        items: articleProvider.fares.map((fare) {
                          return DropdownMenuItem<Fare>(
                            value: fare,
                            child: Row(
                              children: [
                                Icon(
                                  Icons.monetization_on,
                                  color: Colors.blue.shade700,
                                  size: 20,
                                ),
                                SizedBox(width: 12),
                                Expanded(
                                  child: RichText(
                                    text: TextSpan(
                                      style: TextStyle(
                                        fontFamily: 'Montserrat',
                                        fontSize: 16,
                                        color: AppColors.textDark,
                                      ),
                                      children: [
                                        TextSpan(
                                          text: '${fare.percentage}% ',
                                          style: TextStyle(
                                            fontWeight: FontWeight.bold,
                                          ),
                                        ),
                                        TextSpan(
                                          text: fare.description,
                                          style: TextStyle(
                                            fontSize: 14,
                                          ),
                                        ),
                                      ],
                                    ),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ],
                            ),
                          );
                        }).toList(),
                        onChanged: (Fare? value) {
                          setState(() {
                            _selectedFare = value;
                          });
                        },
                      ),
                    ),
                  );
                },
              ),

              _buildPricePreview(),

              SizedBox(height: 16),

              // Description
              TextFormField(
                controller: _descriptionController,
                maxLines: 3,
                decoration: InputDecoration(
                  labelText: 'Descripción',
                  hintText: 'Ingresa una descripción detallada del ${_articleType == 'S' ? 'servicio' : 'producto'}',
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  filled: true,
                  fillColor: Colors.white,
                  floatingLabelBehavior: FloatingLabelBehavior.always,
                ),
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 16,
                ),
              ),

              SizedBox(height: 16),

              // Image URL with toggle option
              Row(
                children: [
                  Switch(
                    value: _includeImage,
                    onChanged: (value) {
                      setState(() {
                        _includeImage = value;
                        if (!value) {
                          _imageController.text = '';
                        }
                      });
                    },
                    activeColor: AppColors.gold,
                  ),
                  Text(
                    'Incluir URL de imagen',
                    style: TextStyle(
                      fontFamily: 'Montserrat',
                      fontSize: 16,
                      color: AppColors.textDark,
                    ),
                  ),
                ],
              ),

              if (_includeImage) ...[
                SizedBox(height: 8),
                TextFormField(
                  controller: _imageController,
                  decoration: InputDecoration(
                    labelText: 'URL de la Imagen',
                    hintText: 'https://ejemplo.com/imagen.jpg',
                    prefixIcon: Icon(Icons.image),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                    filled: true,
                    fillColor: Colors.white,
                    floatingLabelBehavior: FloatingLabelBehavior.always,
                  ),
                  style: TextStyle(
                    fontFamily: 'Montserrat',
                    fontSize: 16,
                  ),
                ),
              ],

              SizedBox(height: 24),

              // Category Selection
              Text(
                'Categoría *',
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textDark,
                ),
              ),
              SizedBox(height: 8),
              Consumer<CategoryProvider>(
                builder: (context, categoryProvider, child) {
                  if (categoryProvider.isLoading) {
                    return CircularProgressIndicator(
                      color: AppColors.gold,
                    );
                  }

                  if (categoryProvider.categories.isEmpty) {
                    return Container(
                      padding: EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: Colors.red.shade50,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: Colors.red.shade200),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'No hay categorías disponibles',
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: Colors.red.shade700,
                            ),
                          ),
                          SizedBox(height: 8),
                          Text(
                            'Por favor, crea al menos una categoría para poder agregar ${_articleType == 'S' ? 'servicios' : 'productos'}.',
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontSize: 14,
                              color: Colors.red.shade700,
                            ),
                          ),
                        ],
                      ),
                    );
                  }

                  return Container(
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: Colors.grey.shade300),
                    ),
                    child: DropdownButtonHideUnderline(
                      child: DropdownButton<int>(
                        value: _selectedCategoryId,
                        isExpanded: true,
                        hint: Padding(
                          padding: EdgeInsets.symmetric(horizontal: 16),
                          child: Text(
                            'Selecciona una categoría',
                            style: TextStyle(
                              fontFamily: 'Montserrat',
                              fontSize: 16,
                              color: Colors.grey.shade600,
                            ),
                          ),
                        ),
                        padding: EdgeInsets.symmetric(horizontal: 16),
                        borderRadius: BorderRadius.circular(12),
                        items: categoryProvider.categories
                            .where((cat) => cat.status == "A")
                            .map((category) {
                          return DropdownMenuItem<int>(
                            value: category.id,
                            child: Row(
                              children: [
                                Icon(
                                  Icons.category,
                                  color: Colors.purple.shade700,
                                  size: 20,
                                ),
                                SizedBox(width: 12),
                                Expanded(
                                  child: Text(
                                    category.name,
                                    style: TextStyle(
                                      fontFamily: 'Montserrat',
                                      fontSize: 16,
                                      color: AppColors.textDark,
                                    ),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ],
                            ),
                          );
                        }).toList(),
                        onChanged: (int? value) {
                          setState(() {
                            _selectedCategoryId = value;
                          });
                        },
                      ),
                    ),
                  );
                },
              ),

              SizedBox(height: 24),

              // Status selection
              Text(
                'Estado',
                style: TextStyle(
                  fontFamily: 'Montserrat',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textDark,
                ),
              ),
              SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: RadioListTile<String>(
                      title: Text(
                        'Activo',
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 16,
                        ),
                      ),
                      value: 'A',
                      groupValue: _status,
                      onChanged: (String? value) {
                        setState(() {
                          _status = value!;
                        });
                      },
                      activeColor: AppColors.gold,
                      contentPadding: EdgeInsets.zero,
                    ),
                  ),
                  Expanded(
                    child: RadioListTile<String>(
                      title: Text(
                        'Inactivo',
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 16,
                        ),
                      ),
                      value: 'I',
                      groupValue: _status,
                      onChanged: (String? value) {
                        setState(() {
                          _status = value!;
                        });
                      },
                      activeColor: AppColors.gold,
                      contentPadding: EdgeInsets.zero,
                    ),
                  ),
                ],
              ),

              SizedBox(height: 32),

              // Save button
              Container(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: _isLoading ? null : _saveArticle,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: _articleType == 'S' ? Colors.green : AppColors.gold,
                    padding: EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                    elevation: 2,
                  ),
                  child: _isLoading
                      ? CircularProgressIndicator(
                    color: Colors.white,
                  )
                      : Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(
                        _articleType == 'S' ? Icons.build : Icons.add_shopping_cart,
                        color: Colors.white,
                      ),
                      SizedBox(width: 8),
                      Text(
                        _articleType == 'S' ? 'Crear Servicio' : 'Crear Producto',
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                        ),
                      ),
                    ],
                  ),
                ),
              ),

              SizedBox(height: 16),

              // Information section based on type
              Container(
                padding: EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: _articleType == 'S'
                      ? Colors.green.shade50
                      : Colors.blue.shade50,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(
                    color: _articleType == 'S'
                        ? Colors.green.shade200
                        : Colors.blue.shade200,
                  ),
                ),
                child: Row(
                  children: [
                    Icon(
                      Icons.info_outline,
                      color: _articleType == 'S'
                          ? Colors.green.shade700
                          : Colors.blue.shade700,
                      size: 20,
                    ),
                    SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        _articleType == 'S'
                            ? 'Los servicios no requieren gestión de inventario físico. El stock se establece automáticamente en 999,999 unidades.'
                            : 'Los productos normales requieren gestión de inventario. Ingresa el stock inicial disponible.',
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 12,
                          color: _articleType == 'S'
                              ? Colors.green.shade700
                              : Colors.blue.shade700,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  // Widget para mostrar preview del precio con IVA
  Widget _buildPricePreview() {
    if (_selectedFare == null || _priceController.text.isEmpty) {
      return SizedBox.shrink();
    }

    final basePrice = double.tryParse(_priceController.text) ?? 0.0;
    if (basePrice <= 0) return SizedBox.shrink();

    final ivaAmount = basePrice * (_selectedFare!.percentage / 100);
    final finalPrice = basePrice + ivaAmount;

    return Container(
      margin: EdgeInsets.symmetric(vertical: 16),
      padding: EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [
            Colors.blue.shade50,
            Colors.indigo.shade50,
          ],
        ),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: Colors.blue.shade200,
          width: 1.5,
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.blue.withOpacity(0.1),
            blurRadius: 10,
            offset: Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        children: [
          // Header con icono
          Row(
            children: [
              Container(
                padding: EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.blue.shade100,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(
                  Icons.calculate,
                  color: Colors.blue.shade700,
                  size: 24,
                ),
              ),
              SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Precio de Venta al Público',
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: Colors.blue.shade800,
                      ),
                    ),
                    Text(
                      'Con ${_selectedFare!.percentage}% de ${_selectedFare!.description}',
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 12,
                        color: Colors.blue.shade600,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),

          SizedBox(height: 16),

          // Cálculo detallado
          Container(
            padding: EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: Colors.grey.shade200),
            ),
            child: Column(
              children: [
                // Precio base
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Row(
                      children: [
                        Icon(
                          Icons.shopping_bag,
                          color: Colors.grey.shade600,
                          size: 18,
                        ),
                        SizedBox(width: 8),
                        Text(
                          'Precio base:',
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 14,
                            color: Colors.grey.shade700,
                          ),
                        ),
                      ],
                    ),
                    Text(
                      '\$${basePrice.toStringAsFixed(2)}',
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 14,
                        fontWeight: FontWeight.w500,
                        color: Colors.grey.shade700,
                      ),
                    ),
                  ],
                ),

                SizedBox(height: 8),

                // IVA
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Row(
                      children: [
                        Icon(
                          Icons.add,
                          color: Colors.orange.shade600,
                          size: 18,
                        ),
                        SizedBox(width: 8),
                        Text(
                          '${_selectedFare!.description} (${_selectedFare!.percentage}%):',
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 14,
                            color: Colors.orange.shade700,
                          ),
                        ),
                      ],
                    ),
                    Text(
                      '\$${ivaAmount.toStringAsFixed(2)}',
                      style: TextStyle(
                        fontFamily: 'Montserrat',
                        fontSize: 14,
                        fontWeight: FontWeight.w500,
                        color: Colors.orange.shade700,
                      ),
                    ),
                  ],
                ),

                SizedBox(height: 12),

                // Divisor
                Container(
                  height: 1,
                  color: Colors.grey.shade300,
                ),

                SizedBox(height: 12),

                // Precio final
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Row(
                      children: [
                        Container(
                          padding: EdgeInsets.all(6),
                          decoration: BoxDecoration(
                            color: Colors.green.shade100,
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Icon(
                            Icons.attach_money,
                            color: Colors.green.shade700,
                            size: 18,
                          ),
                        ),
                        SizedBox(width: 8),
                        Text(
                          'Precio Final:',
                          style: TextStyle(
                            fontFamily: 'Montserrat',
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: Colors.green.shade800,
                          ),
                        ),
                      ],
                    ),
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                      decoration: BoxDecoration(
                        color: Colors.green.shade100,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(
                        '\$${finalPrice.toStringAsFixed(2)}',
                        style: TextStyle(
                          fontFamily: 'Montserrat',
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: Colors.green.shade800,
                        ),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),

          SizedBox(height: 12),

          // Mensaje informativo
          Row(
            children: [
              Icon(
                Icons.info_outline,
                color: Colors.blue.shade600,
                size: 16,
              ),
              SizedBox(width: 8),
              Expanded(
                child: Text(
                  'Este será el precio que verán tus clientes al comprar el ${_articleType == 'S' ? 'servicio' : 'producto'}',
                  style: TextStyle(
                    fontFamily: 'Montserrat',
                    fontSize: 12,
                    color: Colors.blue.shade600,
                    fontStyle: FontStyle.italic,
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}