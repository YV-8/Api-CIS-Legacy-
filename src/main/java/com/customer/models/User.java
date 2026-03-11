package com.customer.models;
import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;


@Entity 
@Table(name = "test_users")
@Data 
@NoArgsConstructor
@AllArgsConstructor

public class User {

    @Id
    @Column(name = "id", length  = 36, nullable = false, unique = true)
    private String id;

    @Column(name = "name", length = 200, nullable = false)
    private String name;

    @Column(name = "login", length = 20, nullable = false, unique = true)
    private String login;
    
    @Column(name = "password", length = 100, nullable = false)
    private String password;
    
}
