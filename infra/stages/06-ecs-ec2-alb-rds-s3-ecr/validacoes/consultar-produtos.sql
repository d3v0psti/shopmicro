SELECT
    "Id",
    "Name",
    "ImageUrl",
    "CreatedAt"
FROM products
ORDER BY "CreatedAt" DESC
LIMIT 10;

SELECT COUNT(*) AS "ProdutosDeValidacao"
FROM products
WHERE "Name" LIKE 'VALIDACAO-STAGE-06%';
