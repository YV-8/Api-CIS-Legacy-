package com.customer.models;
import org.hibernate.annotations.UuidGenerator;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;


@Entity 
@Table(name = "users")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor

public class User {

    @Id
    @UuidGenerator(style = UuidGenerator.Style.RANDOM)
    @Column(name = "id", length  = 36, nullable = false, unique = true)
    private String id;

    @Column(name = "name", length = 200, nullable = false)
    private String name;

    @Column(name = "login", length = 20, nullable = false, unique = true)
    private String login;
    
    @Column(name = "password", length = 100, nullable = false)
    private String password;
    
}
