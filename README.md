# server_d

## 1

- текст шифруется публичным ключом юзера
- далее генерятся n ключей ( по айди книги, юзера, количеству n и timestamp )
- создается бутерброд из слоев En, в каждом откусывается по куску → направляются в сервер
- ключи тоже отправляются клиенту

Юзер просит у сервера откусанную часть последнюю, он выдает ее и удаляет у себя
<br>**только что поняла проблему - клиент далее спокойно может расшифровывать бесконечно один слой!!!! непорядок**
