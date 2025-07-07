# Pasos para transformar un JSON en DTO
Lo primero que tenemos que tener para hacerlo es un JSON
```json
{
  "id": "I-BW452GLLEP1G",
  "plan_id": "P-5ML4271244454362WXNWU5NQ",
  "status": "ACTIVE",
  "start_time": "2025-07-07T10:00:00Z",
  "status_update_time": "2025-07-07T10:05:00Z",
  "subscriber": {
    "name": {
      "given_name": "John",
      "surname": "Doe"
    },
    "email_address": "john.doe@example.com",
    "payer_id": "QYR6XZY4JAD7Y"
  },
  "billing_info": {
    "outstanding_balance": {
      "currency_code": "USD",
      "value": "0.00"
    },
    "next_billing_time": "2025-08-07T10:00:00Z",
    "last_payment": {
      "amount": {
        "currency_code": "USD",
        "value": "10.00"
      },
      "time": "2025-07-07T10:00:00Z"
    },
    "final_payment_time": null,
    "cycle_executions": [
      {
        "cycles_completed": 1,
        "cycles_remaining": 11,
        "total_cycles": 12
      }
    ]
  }
}
````
Cuando tenemos el JSON lo analizamos y vemos cuales son las propiedades **raiz** que son las que se encuentran a lo primero del JSON
que solo estan envueltas en la primera llave estas **propiedades** formaran la clase raiz que se puede llamar:
```csharp

public class PaypalSubscriptionResponse {
 public string id {get;set;}
 public string plan_id {get; set;}
 public string status {get; set;}
 public DateTime? start_time {get;set;}
 public DateTime? status_update_time {get;set;}
}
````
Estas propiedades que hemos puesto primero son las que estan en la raiz llegamos al primer objeto del JSON para transformarlo en DTO cada objeto
creara una clase nueva:
```csharp

public class PaypalSubscriptionResponse {
 //propiedades anteriores
 public Subscriber subscriber {get;set;}
}
public class Subscriber{

}
````
Aqui encontramos un caso especial vemos que el objeto susbcriber tiene sus propiedades y ademas tiene otro objeto anidado pues para ello creamos su clase
correspondiente
```csharp

public class PaypalSubscriptionResponse {
 //propiedades anteriores
 public Subscriber subscriber {get;set;}
}
public class Subscriber{
  public  Name name {get; set;}
}
public class Name{
 public string given_name {get;set}
 public string surname {get;set}

}
````
Ahora que hemos hecho la clase anidada dentro de Subscriber veamos como llevamos has ahora la transformacion de JSON a DTO
```csharp

public class PaypalSubscriptionResponse {
 public string id {get;set;}
 public string plan_id {get; set;}
 public string status {get; set;}
 public DateTime? start_time {get;set;}
 public DateTime? status_update_time {get;set;}
 public Subscriber subscriber {get;set;}
}
public class Subscriber{
  public  Name name {get; set;}
  public string email_address {get;set;}
  public string payer_id {get; set;}
}
public class Name{
 public string given_name {get;set}
 public string surname {get;set}

}
````
Lleagados a este punto se puede decir que hemos echo  la mitad de la transformacion en DTO pero todavia queda la siguiente parte veamos como continuar
nos quedamos aqui:
```csharp

public class PaypalSubscriptionResponse {
 public string id {get;set;}
 public string plan_id {get; set;}
 public string status {get; set;}
 public DateTime? start_time {get;set;}
 public DateTime? status_update_time {get;set;}
 public Subscriber subscriber {get;set;}
}
public class Subscriber{
  public  Name name {get; set;}
  public string email_address {get;set;}
  public string payer_id {get; set;}
}
public class Name{
 public string given_name {get;set}
 public string surname {get;set}

}
````
Pues como esta dentro del JSON raiz pues vamos a la clase que representa la raiz del json y creamos una clase que represente ese objeto:
```csharp

public class PaypalSubscriptionResponse {
 public string id {get;set;}
 public string plan_id {get; set;}
 public DateTime? start_time {get;set;}
 public DateTime? status_update_time {get;set;}
 public Subscriber subscriber {get;set;}
 public  BillingInfo billing_info {get; set;}
}
public class Subscriber{
  public  Name name {get; set;}
  public string email_address {get;set;}
  public string payer_id {get; set;}
}
public class Name{
 public string given_name {get;set}
 public string surname {get;set}

}
public class BillingInfo{


}
````
Llegados a este punto vemos que tenemos el mismo caso que subscriber pues lo que hacemos es hacer lo mismo crear una clase para el objeto anidado
```csharp

public class PaypalSubscriptionResponse {
 public string id {get;set;}
 public string plan_id {get; set;}
 public string status {get; set;}
 public DateTime? start_time {get;set;}
 public DateTime? status_update_time {get;set;}
 public Subscriber subscriber {get;set;}
 public  BillingInfo billing_info {get; set;}
}
public class Subscriber{
  public  Name name {get; set;}
  public string email_address {get;set;}
  public string payer_id {get; set;}
}
public class Name{
 public string given_name {get;set}
 public string surname {get;set}

}
public class BillingInfo{
  public  Amount outstanding_balance {get;set;}

}
public class Amount {
public string currency_code {get;set;}
public string value {get;set;}
}
````
Puedes preguntarte ¿porque le ponemos amount de nombre? es sencillo el nombre del JSON  es complicado y viendo las propiedades que tiene se puede deducir lo 
que hace, continuemos con la trasnformacion
```csharp

public class PaypalSubscriptionResponse {
 public string id {get;set;}
 public string plan_id {get; set;}
 public string status {get; set;}
 public DateTime? start_time {get;set;}
 public DateTime? status_update_time {get;set;}
 public Subscriber subscriber {get;set;}
 public  BillingInfo billing_info {get; set;}
}
public class Subscriber{
  public  Name name {get; set;}
  public string email_address {get;set;}
  public string payer_id {get; set;}
}
public class Name{
 public string given_name {get;set}
 public string surname {get;set}

}
public class BillingInfo{
  public  Amount outstanding_balance {get;set;}
  public DateTime? next_billing_time {get;set;}

}
public class Amount {
public string currency_code {get;set;}
public string value {get;set;}
}
````
Como vemos dentro del objeto billing_info tenemos varios niveles de anidamiento como hemos echo hasta ahora cada objeto del json crea una clase

```csharp

public class PaypalSubscriptionResponse {
 public string id {get;set;}
 public string plan_id {get; set;}
 public string status {get; set;}
 public DateTime? start_time {get;set;}
 public DateTime? status_update_time {get;set;}
 public Subscriber subscriber {get;set;}
 public  BillingInfo billing_info {get; set;}
}
public class Subscriber{
  public  Name name {get; set;}
  public string email_address {get;set;}
  public string payer_id {get; set;}
}
public class Name{
 public string given_name {get;set}
 public string surname {get;set}

}
public class BillingInfo{
  public  Amount outstanding_balance {get;set;}
  public DateTime? next_billing_time {get;set;}
  public  LastPayment last_payment {get;set;}
  public  DateTime? final_payment_time {get;set;}
}
public class LastPayment{
public  Amount amount {get;set;}
public DateTime? time {get;set;}
}
public class Amount {
public string currency_code {get;set;}
public string value {get;set;}
}
````
Llegados a este punto tenemos casi terminada la transformacion pero nos topamos con un caso especial que es cuando un json tiene un array de objetos
cuando esto ocurre en lo que se transforma ese array y eso se mete en un DTO se pone como una lista.


```csharp

public class PaypalSubscriptionResponse {
 public string id {get;set;}
 public string plan_id {get; set;}
 public string status {get; set;}
 public DateTime? start_time {get;set;}
 public DateTime? status_update_time {get;set;}
 public Subscriber subscriber {get;set;}
 public  BillingInfo billing_info {get; set;}
}
public class Subscriber{
  public  Name name {get; set;}
  public string email_address {get;set;}
  public string payer_id {get; set;}
}
public class Name{
 public string given_name {get;set}
 public string surname {get;set}

}
public class BillingInfo{
  public  Amount outstanding_balance {get;set;}
  public DateTime? next_billing_time {get;set;}
  public  LastPayment last_payment {get;set;}
  public  DateTime? final_payment_time {get;set;}
  public  List<CycleExecution> cycle_executions {get;set;}
}
public class LastPayment{
public  Amount amount {get;set;}
public DateTime? time {get;set;}
}
public class Amount {
public string currency_code {get;set;}
public string value {get;set;}
}
public class CycleExecution{
public int cycles_completed {get;set;}
public int cycles_remaining {get; set;}
public int total_cycles {get; set;}
}
````
Como vemos este es el resultado final de la transformacion de json a dto ahora que tenemos el dto este puede deserializar el json si sigue exactamente
esta estructura el como se deserialice por detras no nos meteremos muy afondo en esta guia solo necesitan saber que para deserializar el json de esta forma
hay que cumplir 2 reglas fundamentales:
 1- QUE LAS PROPIEDADES TENGAN EL MISMO NOMBRE
 2- TENER CUIDADO CON MAYUSCULAS Y MINUSCULAS

 ```csharp


  var authToken = await GetAccessTokenAsync();
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/billing/subscriptions/{subscription_id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<PaypalSubscriptionResponse>(responseContent);
                }
                else
                {
                    throw new Exception($"Error al obtener los detalles de la suscripción: {responseContent}");
                }
            }

````
 La deserializacion se hace con (JsonConvert.DeserializeObject<PaypalSubscriptionResponse>(responseContent); esto lo que hace es por decirlo de un modo tipar
 el json pero ojo para eso las propiedades del dto tienen que tener el mismo nombre y tener cuidado con mayuscula y minuscula porque esto lo que hace es como 
 un **match** y si coincide todo se deserializa correctamente en caso de haber alguna diferencia se producira una excepcion.

 Campos como start_time, status_update_time, next_billing_time, final_payment_time, y time pueden ser null en el JSON (e.g., "final_payment_time": null).
 Por eso, usamos DateTime? en el DTO. En el mapeo a la entidad de dominio, usamos el operador ?? para asignar valores predeterminados,
 como new DateTime(1753, 1, 1).

 Clases como Subscriber, BillingInfo, LastPayment, Name, Amount, y List<CycleExecution> son tipos de referencia en 
 C#, por lo que son anulables por defecto y pueden ser null si el JSON no incluye esos objetos o los marca como null. En el mapeo a la entidad de 
 dominio, usamos el operador ?? para asignar valores predeterminados, como new DateTime(1753, 1, 1)."


 ¿Que es un tipo de referencia?
 Un tipo de referencia es un tipo de dato que almacena una referencia (o dirección) a un objeto en la memoria, en lugar de almacenar el 
 objeto completo directamente. Esto significa que la variable no contiene el valor en sí, sino un "puntero" al lugar en la memoria donde está el objeto.
 Ejemplos de tipos de referencia incluyen:Clases (como Subscriber, BillingInfo, LastPayment, Name, Amount, y CycleExecution en tu DTO).
Interfaces (como IEnumerable, IList).
Cadenas (string).
Arreglos (como string[], int[]).
Listas (como List<T>, e.g., List<CycleExecution> en tu DTO).
Característica clave: Los tipos de referencia son anulables por defecto, lo que significa que pueden tener el valor null. 
Por ejemplo, si el JSON tiene "subscriber": null, la propiedad public Subscriber subscriber { get; set; } se deserializará como null sin problemas.


Todos estos tipos de datos complejos se almacenan en el heap y en el stack se almacena un referencia a ellos



Tipos de valor:Un tipo de valor almacena el valor real directamente en la variable, no una referencia a la memoria. Esto significa que la variable contiene el dato en sí.
Ejemplos de tipos de valor incluyen:Estructuras (como DateTime, int, decimal, bool).
Enumeraciones (como enum).

Característica clave: Los tipos de valor no son anulables por defecto. Por ejemplo, un DateTime siempre debe tener un valor (como 01/01/0001 00:00:00 si no se inicializa explícitamente). Para hacer que un tipo de valor sea anulable, usas el operador ? (e.g., DateTime?, int?), que envuelve el tipo en una estructura Nullable<T>.
Ubicación en memoria: Los tipos de valor se almacenan en el stack (pila) o dentro de un objeto en el heap si son parte de una clase.

