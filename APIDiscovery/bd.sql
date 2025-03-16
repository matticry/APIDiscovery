create table tbl_empresa
(
    id_empresa     int identity
        constraint tbl_empresa_pk
            primary key,
    name_empresa   varchar(250) not null,
    status_empresa char        default 'A',
    created_at     datetime    default getdate(),
    phone_empresa  varchar(10) default '9999999999',
    ruc_empresa    varchar(13) default 'NO DISPONIBLE'
)
go

create table tbl_rol
(
    id_rol     int identity
        constraint tbl_rol_pk
            primary key,
    name_rol   varchar(250) default 'User' not null,
    status_rol char         default 'A'
)
go

create table tbl_user
(
    id_us       int identity
        constraint tbl_user_pk
            primary key,
    name_us     varchar(250) not null,
    lastname_us varchar(250) not null,
    email_us    varchar(250),
    password_us varchar(250) not null,
    created_at  datetime    default getdate(),
    google_id   int,
    id_rol      int         default 1
        constraint tbl_user_tbl_rol_id_rol_fk
            references tbl_rol,
    id_empresa  int         default 1
        constraint tbl_user_tbl_empresa_id_empresa_fk
            references tbl_empresa,
    dni_us      varchar(10) default '1000000000',
    image_us    varchar(250),
    status_us   char        default 'A'
)
go

create table tbl_tokens
(
    id_token   int identity
        primary key,
    user_id    int           not null
        references tbl_user,
    token      nvarchar(max) not null,
    created_at datetime default getdate(),
    expires_at datetime      not null
)
go




create table tbl_cursos
(
    id_cu         int identity
        constraint tbl_cursos_pk
            primary key,
    name_cu       varchar(250) collate SQL_Latin1_General_CP1_CI_AS,
    status_cu     char default 'A' collate SQL_Latin1_General_CP1_CI_AS,
    start_date_cu datetime,
    end_date_cu   datetime
)
go

create table tbl_departamento
(
    id_depa     int identity
        constraint tbl_departamento_pk
            primary key,
    name_depa   varchar(250) not null collate SQL_Latin1_General_CP1_CI_AS,
    status_depa char     default 'A' collate SQL_Latin1_General_CP1_CI_AS,
    created_at  datetime default getdate()
)
go

create table tbl_empresa
(
    id_empresa     int identity
        constraint tbl_empresa_pk
            primary key,
    name_empresa   varchar(250) not null collate SQL_Latin1_General_CP1_CI_AS,
    status_empresa char        default 'A' collate SQL_Latin1_General_CP1_CI_AS,
    created_at     datetime    default getdate(),
    phone_empresa  varchar(10) default '9999999999' collate SQL_Latin1_General_CP1_CI_AS,
    ruc_empresa    varchar(13) default 'NO DISPONIBLE' collate SQL_Latin1_General_CP1_CI_AS
)
go

create table tbl_familiares
(
    id_fam         int identity
        constraint tbl_familiares_pk
            primary key,
    name_fam       varchar(250) not null collate SQL_Latin1_General_CP1_CI_AS,
    parentesco_fam char collate SQL_Latin1_General_CP1_CI_AS,
    created_at     datetime default getdate()
)
go

create table tbl_report
(
    id_re         int identity
        constraint tbl_report_pk
            primary key,
    action_re     varchar(250) not null collate SQL_Latin1_General_CP1_CI_AS,
    created_at_re datetime     default getdate(),
    user_re       varchar(250) default 'Josue Ulloa' collate SQL_Latin1_General_CP1_CI_AS,
    dni_re        varchar(50)  default '1755386099' collate SQL_Latin1_General_CP1_CI_AS,
    status_re     char         default 'A' collate SQL_Latin1_General_CP1_CI_AS
)
go

create table tbl_rol
(
    id_rol     int identity
        constraint tbl_rol_pk
            primary key,
    name_rol   varchar(250) default 'User' not null collate SQL_Latin1_General_CP1_CI_AS,
    status_rol char         default 'A' collate SQL_Latin1_General_CP1_CI_AS
)
go

create table tbl_user
(
    id_us       int identity
        constraint tbl_user_pk
            primary key,
    name_us     varchar(250) not null collate SQL_Latin1_General_CP1_CI_AS,
    lastname_us varchar(250) not null collate SQL_Latin1_General_CP1_CI_AS,
    email_us    varchar(250) collate SQL_Latin1_General_CP1_CI_AS,
    password_us varchar(250) not null collate SQL_Latin1_General_CP1_CI_AS,
    created_at  datetime    default getdate(),
    google_id   int,
    id_rol      int         default 1
        constraint tbl_user_tbl_rol_id_rol_fk
            references tbl_rol,
    id_empresa  int         default 1
        constraint tbl_user_tbl_empresa_id_empresa_fk
            references tbl_empresa,
    dni_us      varchar(10) default '1000000000' collate SQL_Latin1_General_CP1_CI_AS,
    image_us    varchar(250) collate SQL_Latin1_General_CP1_CI_AS,
    status_us   char        default 'A' collate SQL_Latin1_General_CP1_CI_AS,
    birthday_us datetime,
    id_dep      int
        constraint tbl_user_tbl_departamento_id_depa_fk
            references tbl_departamento
)
go

create table tbl_capacitacion
(
    id_cap           int identity
        constraint tbl_capacitacion_pk
            primary key,
    id_usu           int
        constraint tbl_capacitacion_tbl_user_id_us_fk
            references tbl_user,
    fechaIngreso_cap datetime default getdate()
)
go

create table tbl_tokens
(
    id_token   int identity
        primary key,
    user_id    int           not null
        references tbl_user,
    token      nvarchar(max) not null collate SQL_Latin1_General_CP1_CI_AS,
    created_at datetime default getdate(),
    expires_at datetime      not null
)
go

create table tbl_user_familiares
(
    id_user_fam int identity
        constraint tbl_user_familiares_pk
            primary key,
    id_us       int not null
        constraint tbl_user_familiares_tbl_user_id_us_fk
            references tbl_user,
    id_fam      int not null
        constraint tbl_user_familiares_tbl_familiares_id_fam_fk
            references tbl_familiares,
    created_at  datetime default getdate(),
    status_rel  char     default 'A' collate SQL_Latin1_General_CP1_CI_AS collate SQL_Latin1_General_CP1_CI_AS
)
go

create index idx_user_familiares_id_us
    on tbl_user_familiares (id_us)
go

create index idx_user_familiares_id_fam
    on tbl_user_familiares (id_fam)
go

create table tbl_usu_cu
(
    id_u_c     int identity
        constraint tbl_usu_cu_pk
            primary key,
    id_usu     int
        constraint tbl_usu_cu_tbl_user_id_us_fk
            references tbl_user,
    id_cu      int
        constraint tbl_usu_cu_tbl_cursos_id_cu_fk
            references tbl_cursos,
    created_at datetime default getdate()
)
go

create table provincia
(
    id_provincia          int 
        primary key,
    nombre_provincia      varchar(255)                    null,
    capital_provincia     varchar(255)                    null,
    descripcion_provincia varchar(1000)                   null,
    poblacion_provincia   decimal(10, 2) default 0.00     null,
    superficie_provincia  decimal(10, 2)                  null,
    latitud_provincia     decimal(9, 6)  default 0.000000 null,
    longitud_provincia    decimal(9, 6)  default 0.000000 null,
    id_region             int                             null
);

create table tbl_region
(
    id_region     int 
        primary key,
    nombre_region varchar(255) null
);