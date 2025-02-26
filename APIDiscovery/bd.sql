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
    created_at  datetime default getdate(),
    google_id   int,
    id_rol      int
        constraint tbl_user_tbl_rol_id_rol_fk
            references tbl_rol,
    id_empresa  int
        constraint tbl_user_tbl_empresa_id_empresa_fk
            references tbl_empresa,
    dni_us      varchar(10),
    image_us    varchar(250)
)
go

