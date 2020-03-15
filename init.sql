\connect mummybot

create table if not exists tags
(
    id         bigserial                           not null,
    name       varchar(12)                         not null,
    content    varchar(255)                        not null,
    author     bigint                              not null,
    guild      bigint                              not null,
    createdat  timestamp default CURRENT_TIMESTAMP not null,
    iscommand  boolean   default false             not null,
    uses       integer   default 0                 not null,
    lastusedby bigint,
    lastused   timestamp,
    constraint tags_pkey
        primary key (id)
);

create table if not exists users
(
    id        bigserial   not null,
    userid    bigint      not null,
    username  varchar(32) not null,
    nickname  varchar(32),
    guildid   bigint      not null,
    avatar    text,
    tagbanned boolean default false,
    joined    timestamp,
    constraint users_pkey
        primary key (id)
);

create table if not exists users_audit
(
    id        bigserial not null,
    userid    bigint    not null,
    username  varchar(32),
    nickname  varchar(32),
    guildid   bigint,
    changedon timestamp default CURRENT_TIMESTAMP,
    constraint users_audit_pkey
        primary key (id)
);

create table if not exists guilds
(
    id            bigserial                                                   not null,
    guildid       bigint                                                      not null,
    guildname     varchar(100)                                                not null,
    ownerid       bigint                                                      not null,
    active        boolean      default true                                   not null,
    region        varchar(25)                                                 not null,
    greeting      varchar(100) default '%user% has joined'::character varying not null,
    goodbye       varchar(100) default '%user% has left'::character varying   not null,
    greetchl      bigint,
    filterinvites boolean      default false                                  not null,
    regex         text         default null,
    constraint guilds_pkey
        primary key (id)
);

create unique index if not exists guilds_guildid_idx
    on guilds (guildid);

create table if not exists blacklist
(
    id        serial                              not null,
    userid    bigint                              not null,
    reason    text,
    createdat timestamp default CURRENT_TIMESTAMP not null,
    constraint blacklist_pk
        primary key (id)
);

create unique index if not exists blacklist_userid_uindex
    on blacklist (userid);

