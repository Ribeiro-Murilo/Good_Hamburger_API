use goodhamburger_db;


INSERT INTO tb_tipo_itens_cardapio VALUES 
(1,'laches',1),
(2,'acompanhamentos',1),
(3,'bebidas',1);


INSERT INTO tb_itens_cardapio VALUES 
(1,'X Burger',1,1,5.50),
(2,'X Bacon',1,1,7.00),
(3,'X Egg',1,1,4.50),
(4,'Batata Frita',2,1,2.00),
(5,'Refrigerante',3,1,2.50);

INSERT INTO `goodhamburger_db`.`tb_descontos` (`Id`, `DescontoPorCento`, `Ativo`) VALUES ('1', '20', '1');
INSERT INTO `goodhamburger_db`.`tb_descontos` (`Id`, `DescontoPorCento`, `Ativo`) VALUES ('2', '15', '1');
INSERT INTO `goodhamburger_db`.`tb_descontos` (`Id`, `DescontoPorCento`, `Ativo`) VALUES ('3', '10', '1');
