package com.user.management.models;

import com.user.management.enums.Role;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import org.springframework.data.annotation.Id;
import org.springframework.data.mongodb.core.index.Indexed;
import org.springframework.data.mongodb.core.mapping.Document;
import org.springframework.data.mongodb.core.mapping.Field;

@Document(collection = "users")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class UserDocument {

    @Id
    private String id;

    @Field("name")
    private String name;

    @Indexed(unique = true)
    @Field("login")
    private String login;

    @Field("password")
    private String password;

    @Field("role")
    private Role role;
}
