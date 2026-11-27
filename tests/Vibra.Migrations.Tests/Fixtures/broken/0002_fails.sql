-- La primera sentencia es válida; la segunda falla.
CREATE TABLE public."Half" ("Id" int);
SELECT 1 / 0;
