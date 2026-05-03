CREATE TABLE IF NOT EXISTS users (
    id       VARCHAR(36)  NOT NULL UNIQUE,
    name     VARCHAR(200) NOT NULL,
    login    VARCHAR(20)  NOT NULL UNIQUE,
    password VARCHAR(100) NOT NULL,
    role     VARCHAR(50)  NOT NULL,
    PRIMARY KEY (id)
);
